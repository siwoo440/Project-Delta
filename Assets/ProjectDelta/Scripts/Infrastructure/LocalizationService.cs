using System.Collections;
using UnityEngine.Localization.Settings;

namespace ProjectDelta.Infrastructure
{
    public sealed class LocalizationService : ILocalizationService
    {
        public IEnumerator InitializeRoutine()
        {
            yield return LocalizationSettings.InitializationOperation;
        }
    }
}
