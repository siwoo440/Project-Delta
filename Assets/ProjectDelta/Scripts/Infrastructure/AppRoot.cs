using System.Collections;
using UnityEngine;
using ProjectDelta.Application;

namespace ProjectDelta.Infrastructure
{
    public sealed class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }

        public ServiceRegistry Services { get; private set; }

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

            // TODO Day 3+: Settings load, Localization, Input, Audio initialization here
            log.Info("Settings load skipped (not implemented yet)");
            log.Info("Localization init skipped (not implemented yet)");
            log.Info("Input init skipped (not implemented yet)");
            log.Info("Audio init skipped (not implemented yet)");

            // TODO Day 4~18: Save system, profile load
            log.Info("Save system init skipped (not implemented yet)");
            log.Info("Profile load skipped (not implemented yet)");

            // TODO Day 3: Addressables init
            log.Info("Addressables init skipped (not implemented yet)");

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
