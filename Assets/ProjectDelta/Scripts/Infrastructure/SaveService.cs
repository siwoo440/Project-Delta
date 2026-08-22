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
    }
}
