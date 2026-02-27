using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;

namespace MapWizard.Desktop.Services;

internal static class FallbackClientIpc
{
    private const string PipeName = "mtipc";
    private static readonly Lock Sync = new();
    private static NamedPipeClientStream? _pipeClient;
    private static Socket? _socketClient;

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public static bool TryReadBeatmapPath(out string? beatmapPath, out string? error)
    {
        beatmapPath = null;
        error = null;

        try
        {
            using var reader = SendMessage(MessageType.ReadBeatmap);
            var (containingFolder, filename) = ReadBeatmapPathPayload(reader);

            if (string.IsNullOrWhiteSpace(filename))
            {
                error = "No beatmap is currently loaded.";
                return false;
            }

            beatmapPath = Path.IsPathRooted(filename)
                ? filename
                : Path.Combine(containingFolder, filename);

            if (string.IsNullOrWhiteSpace(beatmapPath))
            {
                error = "Unable to resolve beatmap path from fallback IPC.";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            InvalidateConnection();
            error = exception.Message;
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public static bool TryReadEditorTime(out int timestamp, out string? error)
    {
        timestamp = 0;
        error = null;

        try
        {
            using var reader = SendMessage(MessageType.EditorTime);
            timestamp = reader.ReadInt32();
            return true;
        }
        catch (Exception exception)
        {
            InvalidateConnection();
            error = exception.Message;
            return false;
        }
    }

    private static BinaryReader SendMessage(MessageType messageType) => OperatingSystem.IsWindows()
        ? SendMessageWindows(messageType)
        : SendMessageLinux(messageType);

    [SupportedOSPlatform("windows")]
    private static BinaryReader SendMessageWindows(MessageType messageType)
    {
        lock (Sync)
        {
            EnsureConnection();

            var payload = BitConverter.GetBytes((int)messageType);
            _pipeClient!.Write(payload, 0, payload.Length);

            var buffer = new byte[1024];
            var bytes = new List<byte>();
            do
            {
                var count = _pipeClient.Read(buffer, 0, buffer.Length);
                if (count <= 0)
                {
                    throw new EndOfStreamException("Fallback IPC closed the pipe while reading.");
                }

                bytes.AddRange(buffer.Take(count));
            } while (!_pipeClient.IsMessageComplete);

            return new BinaryReader(new MemoryStream(bytes.ToArray()));
        }
    }

    private static BinaryReader SendMessageLinux(MessageType messageType)
    {
        lock (Sync)
        {
            EnsureConnection();

            var payload = BitConverter.GetBytes((int)messageType);
            _socketClient!.Send(BitConverter.GetBytes(payload.Length));
            _socketClient.Send(payload);

            var buffer = new byte[1024];
            var bytes = new List<byte>();
            using var stream = new NetworkStream(_socketClient);
            do
            {
                var count = stream.Read(buffer, 0, buffer.Length);
                if (count <= 0)
                {
                    throw new EndOfStreamException("Fallback IPC closed the socket while reading.");
                }

                bytes.AddRange(buffer.Take(count));
            } while (bytes.Count < 4 || bytes.Count < BitConverter.ToInt32(bytes.Take(4).ToArray()) + 4);

            return new BinaryReader(new MemoryStream(bytes.Skip(4).ToArray()));
        }
    }

    private static (string ContainingFolder, string Filename) ReadBeatmapPathPayload(BinaryReader reader)
    {
        var sliderMultiplier = reader.ReadDouble();
        var sliderTickRate = reader.ReadDouble();
        var approachRate = reader.ReadSingle();
        var circleSize = reader.ReadSingle();
        var hpDrainRate = reader.ReadSingle();
        var overallDifficulty = reader.ReadSingle();
        var containingFolder = reader.ReadString();
        var filename = reader.ReadString();
        var previewTime = reader.ReadInt32();
        var stackLeniency = reader.ReadSingle();
        var timelineZoom = reader.ReadSingle();
        
        // this gonna be unused, for now.
        _ = sliderMultiplier;
        _ = sliderTickRate;
        _ = approachRate;
        _ = circleSize;
        _ = hpDrainRate;
        _ = overallDifficulty;
        _ = previewTime;
        _ = stackLeniency;
        _ = timelineZoom;

        return (containingFolder, filename);
    }

    private static void EnsureConnection()
    {
        if (OperatingSystem.IsWindows())
        {
            EnsureConnectionWindows();
        }
        else
        {
            EnsureConnectionLinux();
        }
    }
    
    [SupportedOSPlatform("windows")]
    private static void EnsureConnectionWindows()
    {
        if (_pipeClient is { IsConnected: true })
        {
            return;
        }

        _pipeClient?.Dispose();
        _pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
        _pipeClient.Connect(1000);
        _pipeClient.ReadMode = PipeTransmissionMode.Message;

        using var reader = SendMessage(MessageType.Hello);
        if (reader.ReadInt32() != 1337)
        {
            throw new InvalidOperationException("Fallback IPC returned an unexpected hello result.");
        }
    }

    private static void EnsureConnectionLinux()
    {
        if (_socketClient is { Connected: true })
        {
            return;
        }

        _socketClient?.Dispose();
        _socketClient = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        _socketClient.Connect(new UnixDomainSocketEndPoint($"/tmp/{PipeName}.sock"));

        using var reader = SendMessage(MessageType.Hello);
        if (reader.ReadInt32() != 1337)
        {
            throw new InvalidOperationException("Fallback IPC returned an unexpected hello result.");
        }
    }

    private static void InvalidateConnection()
    {
        lock (Sync)
        {
            _pipeClient?.Dispose();
            _pipeClient = null;
            
            _socketClient?.Dispose();
            _socketClient = null;
        }
    }

    private enum MessageType
    {
        Hello = 0,
        ReadBeatmap = 3,
        EditorTime = 13,
    }
}
