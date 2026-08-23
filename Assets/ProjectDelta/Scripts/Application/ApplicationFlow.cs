using System;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    public sealed class ApplicationFlow
    {
        // 24일차: AppRoot(Infrastructure)가 유일하게 생성하는 인스턴스를 Presentation 쪽 씬 UI가
        // Infrastructure를 직접 참조하지 않고도 쓸 수 있도록 공개한다. RunContext.Current와 같은 패턴.
        public static ApplicationFlow Current { get; private set; }

        private readonly ISceneLoaderService _sceneLoader;
        private readonly ILogService _log;
        private string _pendingSceneName; // 24일차: 로딩 화면을 거쳐 이동할 다음 씬

        public ApplicationFlow(ISceneLoaderService sceneLoader, ILogService log)
        {
            _sceneLoader = sceneLoader;
            _log = log;
            Current = this;
        }

        public void EnterTitle()
        {
            _log.Info("Entering TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
        }

        // 24일차: "새 게임" 버튼에서 호출. 새 런을 시작하고 로딩 화면을 거쳐 던전으로 이동한다.
        public void StartNewGame()
        {
            string runId = Guid.NewGuid().ToString();
            _log.Info($"Starting new game: {runId}");
            RunContext.Begin(runId);
            LoadWithLoadingScreen(SceneNames.Dungeon);
        }

        // 24일차: "설정" 버튼에서 호출. 설정 화면은 가벼워서 로딩 화면 없이 바로 이동한다.
        public void OpenSettings()
        {
            _log.Info("Opening SettingsScene");
            _sceneLoader.LoadSingle(SceneNames.Settings);
        }

        // 24일차: 설정 화면 "뒤로가기", 던전 임시 나가기 버튼에서 호출.
        // 진행 중인 런이 있으면 포기 처리(RunContext.End())한 뒤 타이틀로 돌아간다.
        public void ReturnToTitle()
        {
            if (RunContext.Current != null)
            {
                _log.Info("Abandoning current run");
                RunContext.End();
            }

            _log.Info("Returning to TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
        }

        // 24일차: 목적지를 기억해두고 먼저 LoadingScene을 띄운다.
        private void LoadWithLoadingScreen(string targetSceneName)
        {
            _pendingSceneName = targetSceneName;
            _sceneLoader.LoadSingle(SceneNames.Loading);
        }

        // 24일차: LoadingSceneController가 준비 완료 시(버튼 클릭) 호출해서 실제 목적지로 이동한다.
        public void ProceedFromLoadingScreen()
        {
            if (string.IsNullOrEmpty(_pendingSceneName))
            {
                _log.Info("No pending scene to load from LoadingScene, returning to Title");
                _sceneLoader.LoadSingle(SceneNames.Title);
                return;
            }

            string target = _pendingSceneName;
            _pendingSceneName = null;
            _log.Info($"Loading {target} from LoadingScene");
            _sceneLoader.LoadSingle(target);
        }
    }
}
