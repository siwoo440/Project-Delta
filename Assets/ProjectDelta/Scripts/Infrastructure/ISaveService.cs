using ProjectDelta.Data;

namespace ProjectDelta.Infrastructure
{
    // Compression, obfuscation, checksum and atomic temp-file writing are
    // added in later days (기획서 9.6절). Today this reads/writes files directly -
    // domain systems call these methods instead of touching File I/O themselves
    // (10.4절: "도메인 시스템이 파일을 직접 열거나 JSON을 작성하지 않는다").
    public interface ISaveService
    {
        string SerializeProfile(ProfileData profile);
        ProfileData DeserializeProfile(string json);

        string SerializeRun(RunData run, string saveState);
        RunData DeserializeRun(string json);

        string SerializeSettings(SettingsData settings);
        SettingsData DeserializeSettings(string json);

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
