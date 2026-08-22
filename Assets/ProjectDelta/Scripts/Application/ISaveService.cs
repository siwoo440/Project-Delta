using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // Domain systems call these methods instead of touching File I/O or JSON
    // directly (10.4절 원칙). Writes go through a temp-file-then-replace
    // sequence with checksum verification (기획서 9.5절 안전한 파일 쓰기);
    // reads reject files whose checksum doesn't match as corrupted.
    public interface ISaveService
    {
        void WriteProfile(ProfileData profile);
        ProfileData ReadProfile();
        bool HasProfile();

        void WriteRun(RunData run, string saveState);
        RunData ReadRun();
        bool HasRun();
        void DeleteRun();

        void WriteSettings(SettingsData settings);
        SettingsData ReadSettings();
        bool HasSettings();
    }
}
