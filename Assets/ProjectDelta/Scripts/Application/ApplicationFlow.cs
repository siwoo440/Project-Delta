using System;
using ProjectDelta.Data;
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
        private readonly ISaveService _saveService; // 26일차: 던전 진행 상태 저장·불러오기
        private string _pendingSceneName; // 24일차: 로딩 화면을 거쳐 이동할 다음 씬

        public ApplicationFlow(ISceneLoaderService sceneLoader, ILogService log, ISaveService saveService)
        {
            _sceneLoader = sceneLoader;
            _log = log;
            _saveService = saveService;
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
            DungeonSaveMapper.ClearPendingRestore(); // 26일차: 이전 이어하기 시도의 복원 데이터가 새 런에 섞이지 않도록 정리
            RunContext.Begin(runId);
            LoadWithLoadingScreen(SceneNames.Dungeon);
        }

        // 26일차: 저장된 런이 있는지 확인한다. "이어하기" 버튼 표시 여부에 쓴다.
        public bool HasSavedRun()
        {
            return _saveService != null && _saveService.HasRun();
        }

        // 26일차: "이어하기" 버튼에서 호출. 저장된 런을 읽어 RunContext를 복원하고 던전으로 이동한다.
        // 27일차: 타이틀뿐 아니라 던전 안(이미 런이 진행 중인 상태)에서도 부를 수 있게 되어,
        // 그 경우 먼저 진행 중이던 런을 정리한다.
        public void ContinueGame()
        {
            if (_saveService == null || !_saveService.HasRun()) // 저장 서비스·저장 데이터 존재 확인
            {
                _log.Info("이어할 저장 데이터가 없어 새 게임으로 시작합니다"); // 대체 동작 안내
                StartNewGame(); // 새 게임으로 대체
                return; // 이어하기 중단
            }

            if (RunContext.Current != null) // 27일차: 던전 안에서 불러오기 - 이미 진행 중인 런 확인
            {
                RunContext.End(); // 저장 시점으로 덮어쓸 것이므로 지금 진행 중이던 런은 그냥 정리 (별도 저장 없음)
            }

            RunData savedRun = _saveService.ReadRun(); // 저장 데이터 읽기
            RunContext.Begin(savedRun.BasicInfo.RunId); // 저장된 런 식별자로 런 시작
            DungeonSaveMapper.ApplyBasics(RunContext.Current, savedRun); // 층 번호·현재 방 복원
            DungeonSaveMapper.BeginRestore(savedRun); // 방별 복원 데이터 준비 (씬의 각 방이 스스로 조회)

            _log.Info($"저장된 런 이어하기: {savedRun.BasicInfo.RunId}"); // 이어하기 로그 출력
            LoadWithLoadingScreen(SceneNames.Dungeon); // 로딩 화면을 거쳐 던전으로 이동
        }

        // 22/25일차: 방 진입·상자 개봉처럼 진행 상태가 바뀌는 시점에서 호출한다.
        public void SaveDungeonProgress()
        {
            if (_saveService == null || RunContext.Current == null) // 저장 서비스·진행 중인 런 확인
            {
                return; // 저장 대상 없음, 중단
            }

            RunData data = DungeonSaveMapper.BuildFromRunContext(RunContext.Current); // 현재 런 상태를 저장 데이터로 변환
            _saveService.WriteRun(data, "InProgress"); // 저장 실행
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
                _saveService?.DeleteRun(); // 26일차: 로그라이트 관례상 런 포기 = 그 회차 저장 삭제
                DungeonSaveMapper.ClearPendingRestore(); // 남아있을 수 있는 복원 데이터 정리
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
