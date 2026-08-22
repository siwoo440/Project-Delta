using ProjectDelta.Data;

namespace ProjectDelta.Infrastructure
{
    // File I/O (paths, atomic write, backups, transactions) is added in later
    // days (기획서 9장, 10.4절). Today's scope is JSON round-tripping only -
    // domain systems never open files or write JSON directly (10.4절 원칙).
    public interface ISaveService
    {
        string SerializeProfile(ProfileData profile);
        ProfileData DeserializeProfile(string json);

        string SerializeRun(RunData run, string saveState);
        RunData DeserializeRun(string json);

        string SerializeSettings(SettingsData settings);
        SettingsData DeserializeSettings(string json);
    }
}
