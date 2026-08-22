using System.IO;
using Newtonsoft.Json;
using ProjectDelta.Data;

namespace ProjectDelta.Infrastructure
{
    public sealed class SaveService : ISaveService
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public string SerializeProfile(ProfileData profile)
        {
            return JsonConvert.SerializeObject(SaveEnvelope<ProfileData>.Wrap(profile, saveState: null), JsonSettings);
        }

        public ProfileData DeserializeProfile(string json)
        {
            return JsonConvert.DeserializeObject<SaveEnvelope<ProfileData>>(json, JsonSettings).Payload;
        }

        public string SerializeRun(RunData run, string saveState)
        {
            return JsonConvert.SerializeObject(SaveEnvelope<RunData>.Wrap(run, saveState), JsonSettings);
        }

        public RunData DeserializeRun(string json)
        {
            return JsonConvert.DeserializeObject<SaveEnvelope<RunData>>(json, JsonSettings).Payload;
        }

        public string SerializeSettings(SettingsData settings)
        {
            return JsonConvert.SerializeObject(SaveEnvelope<SettingsData>.Wrap(settings, saveState: null), JsonSettings);
        }

        public SettingsData DeserializeSettings(string json)
        {
            return JsonConvert.DeserializeObject<SaveEnvelope<SettingsData>>(json, JsonSettings).Payload;
        }

        public void WriteProfile(ProfileData profile)
        {
            SavePaths.EnsureSaveDirectoryExists();
            File.WriteAllText(SavePaths.ProfilePath, SerializeProfile(profile));
        }

        public ProfileData ReadProfile()
        {
            return DeserializeProfile(File.ReadAllText(SavePaths.ProfilePath));
        }

        public bool HasProfile()
        {
            return File.Exists(SavePaths.ProfilePath);
        }

        public void WriteRun(RunData run, string saveState)
        {
            SavePaths.EnsureSaveDirectoryExists();
            File.WriteAllText(SavePaths.RunPath, SerializeRun(run, saveState));
        }

        public RunData ReadRun()
        {
            return DeserializeRun(File.ReadAllText(SavePaths.RunPath));
        }

        public bool HasRun()
        {
            return File.Exists(SavePaths.RunPath);
        }

        public void DeleteRun()
        {
            if (File.Exists(SavePaths.RunPath))
            {
                File.Delete(SavePaths.RunPath);
            }
        }

        public void WriteSettings(SettingsData settings)
        {
            SavePaths.EnsureSaveDirectoryExists();
            File.WriteAllText(SavePaths.SettingsPath, SerializeSettings(settings));
        }

        public SettingsData ReadSettings()
        {
            return DeserializeSettings(File.ReadAllText(SavePaths.SettingsPath));
        }

        public bool HasSettings()
        {
            return File.Exists(SavePaths.SettingsPath);
        }
    }
}
