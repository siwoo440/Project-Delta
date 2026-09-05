using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectDelta.Application;
using ProjectDelta.Data;

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

            var saveService = new SaveService();
            Services.Register<ISaveService>(saveService);
            log.Info("Save service ready");

            // TODO: 아직 로드한 프로필을 들고 있을 곳(ProfileContext 등)이 없어 값은 버려진다.
            // 무언가 이 값을 실제로 읽어야 하는 시점(타이틀 화면 등)에 보관 지점을 만든다.
            if (saveService.HasProfile())
            {
                saveService.ReadProfile();
                log.Info("Profile loaded");
            }
            else
            {
                saveService.WriteProfile(new ProfileData());
                log.Info("New profile created");
            }

            var addressables = new AddressableService();
            Services.Register<IAddressableService>(addressables);
            yield return addressables.InitializeRoutine();
            log.Info("Addressables service ready");

            // 134일차: 실제 Steamworks 연동 전까지는 로그만 남기는 브릿지를 등록해
            // 도전과제 True 전환 → Steam 호출 흐름 자체는 지금부터 검증할 수 있게 한다.
            var steamAchievementBridge = new NullSteamAchievementBridge(log);
            Services.Register<ISteamAchievementBridge>(steamAchievementBridge);
            log.Info("Steam init skipped (Steamworks not integrated yet, using null achievement bridge)");
            log.Info("Cloud status check skipped (not implemented yet)");

            var sceneLoader = gameObject.AddComponent<SceneLoaderService>();
            Services.Register<ISceneLoaderService>(sceneLoader);
            log.Info("Scene loader service ready");

            _applicationFlow = new ApplicationFlow(sceneLoader, log, saveService, steamAchievementBridge, input);

            // 137일차: 저장된 키 리매핑을 실제 액션에 반영한 뒤 게임을 시작한다.
            _applicationFlow.ApplyKeyBindingsFromSettings();

            yield return null;
        }
    }
}
