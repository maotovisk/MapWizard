using System.Collections.Generic;
using MapWizard.Desktop.Models.Settings;

namespace MapWizard.Desktop.Services;

public interface IAudioPlaybackService
{
    bool LoadSong(string filePath);
    bool PlaySong(int startTimeMs);
    void PauseSong();
    void StopSong();
    int GetSongPositionMs();
    int GetLoadedSongDurationMs();
    bool IsSongPlaying { get; }
    string GetTimingTelemetryStatus();
    string GetSongDebugStatus();

    void SetSongVolume(float volume);
    void SetHitsoundVolume(float volume);
    bool PlayHitsound(string filePath, float volumeMultiplier = 1f);

    IReadOnlyList<AudioOutputDeviceOption> GetAudioOutputDevices();
    string GetSelectedAudioOutputDeviceId();
    bool SetSelectedAudioOutputDevice(string deviceId);
}
