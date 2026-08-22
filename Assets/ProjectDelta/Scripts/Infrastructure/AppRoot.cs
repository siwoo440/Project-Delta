using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectDelta.Application;

namespace ProjectDelta.Infrastructure
{
    public sealed class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }

        public ServiceRegistry Services { get; private set; }

        [SerializeField] private InputActionAsset inputActions;

        private ApplicationFlow _applicationFlow;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Services = new ServiceRegistry();
        }

        private IEnumerator Start()
        {
            yield return InitializeServices();

            _applicationFlow.EnterTitle();
        }

        private IEnumerator InitializeServices()
        {
            var log = new LogService();
            Services.Register<ILogService>(log);
            log.Info("Log service ready");

            // TODO Day 4+: Settings load
            log.Info("Settings load skipped (not implemented yet)");

            var localization = new LocalizationService();
            Services.Register<ILocalizationService>(localization);
            yield return localization.InitializeRoutine();
            log.Info("Localization service ready");

            var input = new InputService(inputActions);
            Services.Register<IInputService>(input);
            input.SetActiveMap(InputMapNames.UI);
            log.Info("Input service ready");

            // TODO Day 3+: Audio initialization
            log.Info("Audio init skipped (not implemented yet)");

            // TODO Day 4~18: Save system, profile load
            log.Info("Save system init skipped (not implemented yet)");
            log.Info("Profile load skipped (not implemented yet)");

            var addressables = new AddressableService();
            Services.Register<IAddressableService>(addressables);
            yield return addressables.InitializeRoutine();
            log.Info("Addressables service ready");

            // TODO: Steam init, Cloud status check
            log.Info("Steam init skipped (not implemented yet)");
            log.Info("Cloud status check skipped (not implemented yet)");

            var sceneLoader = gameObject.AddComponent<SceneLoaderService>();
            Services.Register<ISceneLoaderService>(sceneLoader);
            log.Info("Scene loader service ready");

            _applicationFlow = new ApplicationFlow(sceneLoader, log);

            yield return null;
        }
    }
}
