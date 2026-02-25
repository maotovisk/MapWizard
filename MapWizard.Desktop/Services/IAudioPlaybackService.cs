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

    void SetSongVolume(float volume);
    void SetHitsoundVolume(float volume);
    bool PlayHitsound(string filePath);
}
