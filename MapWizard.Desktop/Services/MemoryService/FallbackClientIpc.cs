using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;

namespace MapWizard.Desktop.Services.MemoryService;

internal static class FallbackClientIpc
{
    private const string PipeName = "mtipc";
    private const string LinuxSocketPath = $"/tmp/{PipeName}.sock";
    private static readonly Lock Sync = new();
    private static NamedPipeClientStream? _pipeClient;
    private static Socket? _socketClient;

    /// <summary>
    /// After a failed request the client is left alone for a while, doubling up to
    /// <see cref="MaxRetryDelay"/>. Some clients accept the connection but drop it on every request
    /// (and notify the player each time), so reconnecting on every poll would spam them.
    /// </summary>
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(1);
    private static DateTime _retryAfterUtc = DateTime.MinValue;
    private static TimeSpan _retryDelay = TimeSpan.Zero;
    private static string? _lastError;

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public static bool TryReadBeatmapPath(out string? beatmapPath, out string? error)
    {
        beatmapPath = null;

        if (!TryRequest(MessageType.ReadBeatmap, ReadBeatmapPathPayload, out var payload, out error))
        {
            return false;
        }

        var (containingFolder, filename) = payload;
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

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    public static bool TryReadEditorTime(out int timestamp, out string? error) =>
        TryRequest(MessageType.EditorTime, reader => reader.ReadInt32(), out timestamp, out error);

    private static bool TryRequest<T>(MessageType messageType, Func<BinaryReader, T> parse, out T value,
        out string? error)
    {
        value = default!;
        error = null;

        lock (Sync)
        {
            if (DateTime.UtcNow < _retryAfterUtc)
            {
                error = _lastError ?? "Fallback client is unavailable.";
                return false;
            }
        }

        if (!IsEndpointAvailable())
        {
            error = "No fallback client is listening.";
            return false;
        }

        try
        {
            using var reader = SendMessage(messageType);
            value = parse(reader);

            lock (Sync)
            {
                _retryDelay = TimeSpan.Zero;
                _lastError = null;
            }

            return true;
        }
        catch (Exception ex)
        {
            InvalidateConnection();
            error = ex.Message;

            lock (Sync)
            {
                _retryDelay = _retryDelay == TimeSpan.Zero
                    ? InitialRetryDelay
                    : TimeSpan.FromTicks(Math.Min(_retryDelay.Ticks * 2, MaxRetryDelay.Ticks));
                _retryAfterUtc = DateTime.UtcNow + _retryDelay;

                // Polling hits the same failure repeatedly (e.g. a stale socket after the game closes).
                if (ex.Message == _lastError)
                {
                    return false;
                }

                _lastError = ex.Message;
            }

            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// Checks for the client's endpoint without connecting, so callers (including background polling)
    /// skip the connect timeout and exception logging when no fallback client is running.
    /// Pipes are enumerated rather than probed because opening one would consume a server instance.
    /// </summary>
    private static bool IsEndpointAvailable()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return Directory.EnumerateFiles(@"\\.\pipe\")
                    .Any(path => string.Equals(Path.GetFileName(path), PipeName, StringComparison.OrdinalIgnoreCase));
            }

            return File.Exists(LinuxSocketPath);
        }
        catch (Exception)
        {
            // Enumeration failures should not hide a working endpoint; let the connect attempt decide.
            return true;
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
        _socketClient.Connect(new UnixDomainSocketEndPoint(LinuxSocketPath));

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
