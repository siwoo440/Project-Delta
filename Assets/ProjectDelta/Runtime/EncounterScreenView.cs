using ProjectDelta.Core; // 조우 도망 판정과 참가자
using UnityEngine; // 실제 조우 화면 객체
using UnityEngine.EventSystems; // 기본 선택 버튼
using UnityEngine.UI; // 화면 배경
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterScreenView : MonoBehaviour // 시험 대상의 조우 선택 화면
    { // 클래스 시작
        private Button fight; // 전투 선택 버튼
        private Button escape; // 조우 단계 도망 버튼
        private Button observe; // 조우 단계 관찰 버튼
        private Button persuade; // 조우 단계 회유 버튼
        private Text description; // 조우 선택과 이동 결과 안내
        private AppRoot owner; // 현재 게임 상태 관리자
        private RoomTravelController travel; // 이전 방 이동 제어
        private string monsterId; // 실제 조우 대상 식별자
        private string monsterName; // 실제 조우 대상 이름
        private readonly System.Random random = new System.Random(); // 조우 도망 주사위
        private bool pendingEscape; // 도망 성공 후 방 이동 재시도 상태
        private bool pendingPersuasion; // 회유 성공 후 조우 완료 재시도 상태
        private bool failedChoice; // 실패한 조우 행동의 전투 재시도 상태
        private bool pendingDefaultSelection; // 입력 관리자 복귀 후 기본 선택 대기
        private int lastActionFrame = -1; // 같은 프레임 중복 선택 차단
        private const string TrialMonsterId = "trial-elite"; // 현재 시험 대상 식별자
        private const int TrialResistance = 10; // 정식 데이터가 없는 시험 저항
        private const int TrialCharm = 10; // 정식 데이터가 없는 시험 매력
        public static System.Func<int, int, int> EscapeRollOverride // 자동 검증용 조우 주사위 지정
        { // 속성 시작
            get; // 지정 주사위 조회
            set; // 검증 주사위 설정
        } // 속성 끝
        public static System.Func<int, int, int> ObserveRollOverride // 자동 검증용 관찰 주사위 지정
        { // 속성 시작
            get; // 지정 주사위 조회
            set; // 검증 주사위 설정
        } // 속성 끝
        public static System.Func<int, int, int> PersuadeRollOverride // 자동 검증용 회유 주사위 지정
        { // 속성 시작
            get; // 지정 주사위 조회
            set; // 검증 주사위 설정
        } // 속성 끝
        public static EncounterScreenView Create(AppRoot owner) // 조우 화면 생성
        { // 생성 시작
            if (EventSystem.current == null) // 화면 입력 관리자 존재 검사
            { // 입력 관리자 누락 조건 시작
                throw new System.InvalidOperationException("An active EventSystem is required."); // 조작 불가능한 조우 화면 차단
            } // 입력 관리자 누락 조건 끝
            var screen = new GameObject("EncounterScreen", typeof(EncounterScreenView)); // 조우 화면 상위 객체
            try // 조우 화면 구성
            { // 화면 구성 시작
                var view = screen.GetComponent<EncounterScreenView>(); // 실제 조우 화면 제어
                view.owner = owner; // 현재 게임 관리자 연결
                var travelObject = GameObject.Find("RoomTravel"); // 현재 방 이동 객체 조회
                view.travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 이전 방 이동 제어 연결
                view.monsterId = view.travel == null ? TrialMonsterId : view.travel.CurrentMonsterId; // 실제 조우 대상 식별자
                view.monsterName = view.travel == null ? "상위 개체" : view.travel.CurrentMonsterName; // 실제 조우 대상 이름
                var canvas = UiFactory.CreateCanvas("EncounterCanvas"); // 조우 전용 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 조우 화면 아래 배치
                canvas.sortingOrder = 100; // 탐험 화면보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 조우 화면 배경
                background.transform.SetParent(canvas.transform, false); // 화면 전체에 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 크기 조회
                rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 화면 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
                rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 조우 화면 배경색
                var accent = new Color(0.62f, 0.87f, 0.79f); // 조우 화면 강조 색상
                if (view.monsterId == "MON_SLIME") // 첫 슬라임 조우 검사
                { // 슬라임 표시 시작
                    CombatScreenView.AddPortrait(canvas.transform, "EncounterPortrait", "BattlePortraits/SlimePrototype", new Vector2(600f, 45f)); // 첫 슬라임 이미지
                } // 슬라임 표시 끝
                else if (view.monsterId == "MON_GOBLIN") // 고블린 조우 검사
                { // 고블린 표시 시작
                    CombatScreenView.AddPortrait(canvas.transform, "EncounterPortrait", "BattlePortraits/GoblinPrototype", new Vector2(600f, 45f)); // 고블린 몬무스 이미지
                } // 고블린 표시 끝
                else // 기존 상위 개체 분기
                { // 상위 표시 시작
                    CombatScreenView.AddPortrait(canvas.transform, "EncounterPortrait", "BattlePortraits/SlimeQueenPrototype", new Vector2(600f, 45f)); // 기존 상위 개체 임시 그림
                } // 상위 표시 끝
                UiFactory.CreateText(canvas.transform, "Header", "조우", 58, new Vector2(0f, 410f), new Vector2(1000f, 90f), Color.white); // 조우 화면 제목
                UiFactory.CreateText(canvas.transform, "Status", view.monsterName + "을 발견했습니다", 30, new Vector2(0f, 295f), new Vector2(1100f, 70f), accent); // 실제 조우 대상 안내
                view.description = UiFactory.CreateText(canvas.transform, "EncounterDescription", "행동을 선택하세요", 26, new Vector2(-160f, 45f), new Vector2(800f, 130f), Color.white); // 현재 선택과 저장 오류 안내
                UiFactory.CreateText(canvas.transform, "EncounterScope", "유혹 · 아이템은 준비 중", 23, new Vector2(0f, -420f), new Vector2(1400f, 60f), accent); // 미구현 선택 안내
                view.fight = UiFactory.CreateButton(canvas.transform, "EncounterFightButton", "전투", new Vector2(-235f, -300f)); // 실제 전투 선택
                view.fight.onClick.AddListener(view.ChooseFight); // 전투 화면 진입 연결
                view.escape = UiFactory.CreateButton(canvas.transform, "EncounterEscapeButton", "도망", new Vector2(235f, -300f)); // 조우 단계 도망 선택
                view.escape.onClick.AddListener(view.AttemptEscape); // 실제 속도 도망 연결
                view.observe = UiFactory.CreateButton(canvas.transform, "EncounterObserveButton", "관찰", new Vector2(470f, -300f)); // 조우 단계 관찰 선택
                view.observe.onClick.AddListener(view.AttemptObserve); // 실제 관찰 판정 연결
                view.persuade = UiFactory.CreateButton(canvas.transform, "EncounterPersuadeButton", "회유", new Vector2(705f, -300f)); // 실제 회유 선택
                view.persuade.onClick.AddListener(view.AttemptPersuade); // 매력과 저항 회유 연결
                view.persuade.interactable = view.travel != null && view.travel.CanCompleteCurrentEncounter(); // 실제 회유 대상 없는 조우 차단
                if (view.travel != null && view.travel.IsGroupEncounter) // 시험 다수 조우 검사
                { // 다수 조우 안내 시작
                    view.persuade.interactable = false; // 미구현 다수 대상 회유 차단
                    view.description.text = view.monsterName + " · 다수 대상 회유는 준비 중"; // 실제 시험 조우 범위 안내
                } // 다수 조우 안내 끝
                if (owner.Observations.Contains(view.monsterId)) // 이미 관찰한 대상 검사
                { // 관찰 완료 조건 시작
                    view.observe.transform.Find("Label").GetComponent<Text>().text = "정보 확인"; // 기록된 정보 다시 보기 안내
                } // 관찰 완료 조건 끝
                canvas.gameObject.AddComponent<CombatActionLayout>().Initialize(canvas.GetComponent<RectTransform>(), view.fight, view.escape, view.observe, view.persuade); // 화면 너비별 조우 버튼 배치
                if (view.travel == null || !view.travel.HasPreviousRoom) // 이전 방 정보 검사
                { // 이전 방 없음 조건 시작
                    view.escape.interactable = false; // 불가능한 도망 선택 차단
                    view.description.text = "이전 방이 없어 도망할 수 없습니다"; // 선택 불가 이유 안내
                } // 이전 방 없음 조건 끝
                view.SelectDefault(); // 키보드와 게임패드 기본 선택
                return view; // 준비된 조우 화면 전달
            } // 화면 구성 끝
            catch // 화면 생성 실패 정리
            { // 실패 정리 시작
                screen.SetActive(false); // 실패한 화면 즉시 숨김
                Destroy(screen); // 불완전한 조우 화면 제거
                throw; // 기존 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void ChooseFight() // 조우에서 일반 전투 선택
        { // 선택 시작
            if (owner == null || pendingEscape || pendingPersuasion) // 현재 게임과 확정된 조우 결과 검사
            { // 전투 차단 조건 시작
                return; // 확정된 도망의 전투 전환 차단
            } // 전투 차단 조건 끝
            owner.StartCombat(); // 일반 전투 진입 요청
        } // 선택 끝
        private void AttemptObserve() // 시험 대상의 관찰과 정보 확인
        { // 관찰 시작
            if (owner == null || owner.Flow.Phase != GamePhase.Encounter || failedChoice || pendingEscape || pendingPersuasion || lastActionFrame == Time.frameCount) // 현재 조우와 중복 선택 검사
            { // 관찰 거절 조건 시작
                return; // 잘못된 관찰 요청 차단
            } // 관찰 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 중복 선택 차단
            if (owner.Observations.Contains(monsterId)) // 이미 관찰한 대상 검사
            { // 정보 확인 조건 시작
                ShowObservedInfo(); // 추가 주사위 없이 기록된 정보 표시
                return; // 중복 관찰 차단
            } // 정보 확인 조건 끝
            if (owner.PendingObservationId != monsterId) // 아직 굴리지 않은 관찰 판정 검사
            { // 관찰 판정 시작
                var playerRoll = ObserveRollOverride == null ? random.Next(1, 21) : ObserveRollOverride(1, 21); // 플레이어 관찰 주사위
                var enemyRoll = ObserveRollOverride == null ? random.Next(1, 21) : ObserveRollOverride(1, 21); // 상대 은폐 주사위
                if (!ObservationRules.TryResolve(CombatScreenView.TrialPlayerSpeed, 0, TrialResistance, 0, playerRoll, enemyRoll, out var success)) // 속도와 저항 판정 검사
                { // 판정 오류 조건 시작
                    description.text = "관찰 판정을 처리할 수 없습니다"; // 잘못된 판정 안내
                    return; // 기존 조우 화면 유지
                } // 판정 오류 조건 끝
                if (!success) // 관찰 실패 검사
                { // 관찰 실패 조건 시작
                    failedChoice = true; // 실패한 조우 행동의 재시도 차단
                    observe.interactable = false; // 관찰 재시도 비활성화
                    escape.interactable = false; // 실패 후 다른 조우 행동 차단
                    persuade.interactable = false; // 실패 후 회유 선택 차단
                    description.text = "관찰 실패 · 전투를 선택하세요"; // 실패와 전투 재시도 안내
                    owner.StartCombat(); // 관찰 실패 후 일반 전투 진입
                    return; // 정보 공개 차단
                } // 관찰 실패 조건 끝
                owner.PendingObservationId = monsterId; // 화면 이동 후에도 성공 판정 보존
            } // 관찰 판정 끝
            if (!owner.TryRecordObservation(monsterId)) // 안전한 대상별 영구 기록 검사
            { // 저장 실패 조건 시작
                description.text = owner.ObservationSaveMessage + (owner.ObservationSaveBlocked ? " 원본 확인 후 재시작 · 미저장 판정 초기화" : " 관찰 선택으로 재시도"); // 실제 오류와 복구 시 미저장 판정 초기화 안내
                return; // 성공한 판정을 다시 굴리지 않고 보존
            } // 저장 실패 조건 끝
            owner.PendingObservationId = string.Empty; // 저장 완료된 관찰 대기 해제
            observe.transform.Find("Label").GetComponent<Text>().text = "정보 확인"; // 관찰 완료 후 정보 확인 안내
            ShowObservedInfo(); // 실제 시험 대상의 1단계 정보 표시
        } // 관찰 끝
        private void ShowObservedInfo() // 기록한 시험 상대의 1단계 정보 표시
        { // 표시 시작
            description.text = "체력 " + CombatScreenView.TrialEnemyHealth + " · 기본 공격 위주 (시험값)"; // 실제 시험 전투의 체력과 공격 성향
        } // 표시 끝
        private void AttemptPersuade() // 시험 대상의 회유와 평화적 조우 종료
        { // 회유 시작
            if (owner == null || owner.Flow.Phase != GamePhase.Encounter || travel == null || failedChoice || pendingEscape || lastActionFrame == Time.frameCount) // 현재 조우와 중복 선택 검사
            { // 회유 거절 조건 시작
                return; // 잘못된 회유 요청 차단
            } // 회유 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 중복 선택 차단
            if (!pendingPersuasion && !travel.CanCompleteCurrentEncounter()) // 회유 대상의 현재 완료 가능 여부 검사
            { // 대상 없음 조건 시작
                description.text = "회유할 대상을 확인할 수 없습니다"; // 잘못된 조우 상태 안내
                return; // 주사위와 다른 조우 선택 보존
            } // 대상 없음 조건 끝
            if (!pendingPersuasion) // 아직 굴리지 않은 회유 판정 검사
            { // 회유 판정 시작
                var playerRoll = PersuadeRollOverride == null ? random.Next(1, 21) : PersuadeRollOverride(1, 21); // 플레이어 회유 주사위
                var enemyRoll = PersuadeRollOverride == null ? random.Next(1, 21) : PersuadeRollOverride(1, 21); // 대상 저항 주사위
                if (!PersuasionRules.TryResolve(TrialCharm, 0, 0, TrialResistance, 0, 0, playerRoll, enemyRoll, out var success)) // 시험 매력과 저항의 판정 검사
                { // 판정 오류 조건 시작
                    description.text = "회유 판정을 처리할 수 없습니다"; // 잘못된 판정 안내
                    return; // 기존 조우 화면 유지
                } // 판정 오류 조건 끝
                if (!success) // 회유 실패 검사
                { // 회유 실패 조건 시작
                    failedChoice = true; // 실패한 회유의 재판정 차단
                    persuade.interactable = false; // 회유 선택 비활성화
                    escape.interactable = false; // 실패 후 도망 선택 차단
                    observe.interactable = false; // 실패 후 관찰 선택 차단
                    description.text = "회유 실패 · 전투를 선택하세요"; // 실패와 전투 재시도 안내
                    owner.StartCombat(); // 회유 실패 후 일반 전투 진입
                    return; // 평화적 완료 차단
                } // 회유 실패 조건 끝
                pendingPersuasion = true; // 성공 판정의 조우 완료 대기
                fight.interactable = false; // 확정된 회유 이후 전투 차단
                escape.interactable = false; // 확정된 회유 이후 도망 차단
                observe.interactable = false; // 확정된 회유 이후 관찰 차단
            } // 회유 판정 끝
            if (!travel.CompleteCurrentEncounter()) // 현재 시험 조우의 평화적 완료 검사
            { // 완료 실패 조건 시작
                description.text = "조우 종료 실패 · 회유를 다시 선택하세요"; // 판정 보존과 재시도 안내
                return; // 성공 판정과 조우 화면 유지
            } // 완료 실패 조건 끝
            owner.EndEncounterAfterEscape(); // 보상 없이 탐험 화면 복귀
        } // 회유 끝
        private void AttemptEscape() // 조우 단계의 도망 판정과 이전 방 이동
        { // 도망 시작
            if (owner == null || owner.Flow.Phase != GamePhase.Encounter || travel == null || failedChoice || pendingPersuasion || lastActionFrame == Time.frameCount) // 현재 조우와 중복 선택 검사
            { // 도망 거절 조건 시작
                return; // 잘못된 도망 요청 차단
            } // 도망 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 중복 선택 차단
            if (!travel.CanReturnToPreviousRoom()) // 이전 방의 현재 도착 가능 여부 검사
            { // 도착 불가 조건 시작
                description.text = "이전 방으로 이동할 수 없습니다"; // 안전하지 않은 도착 안내
                return; // 주사위와 현재 조우 보존
            } // 도착 불가 조건 끝
            if (!pendingEscape) // 아직 굴리지 않은 도망 판정 검사
            { // 속도 판정 시작
                var player = new BattleActor("플레이어", CombatScreenView.TrialPlayerSpeed, true, 0); // 시험 플레이어 속도
                var enemy = new BattleActor(monsterName, CombatScreenView.TrialEnemySpeed, false, 1); // 실제 대상의 시험 속도
                var actors = new[] // 현재 시험 조우 참가자
                { // 참가자 목록 시작
                    player, enemy // 플레이어와 상위 개체
                }; // 참가자 목록 끝
                var playerRoll = EscapeRollOverride == null ? random.Next(1, 21) : EscapeRollOverride(1, 21); // 플레이어의 도망 주사위
                var enemyRoll = EscapeRollOverride == null ? random.Next(1, 21) : EscapeRollOverride(1, 21); // 상대의 추격 주사위
                if (!EncounterEscapeRules.TryResolve(player, actors, 0, playerRoll, enemyRoll, out var success)) // 속도와 주사위 유효성 검사
                { // 판정 오류 조건 시작
                    description.text = "도망 판정을 처리할 수 없습니다"; // 잘못된 판정 안내
                    return; // 조우 화면 유지
                } // 판정 오류 조건 끝
                if (!success) // 도망 실패 검사
                { // 도망 실패 조건 시작
                    failedChoice = true; // 실패한 도망의 재시도 차단
                    escape.interactable = false; // 도망 선택 비활성화
                    observe.interactable = false; // 실패 후 다른 조우 행동 차단
                    persuade.interactable = false; // 실패 후 회유 선택 차단
                    description.text = "도망 실패 · 전투를 선택하세요"; // 실패와 전투 재시도 안내
                    owner.StartCombat(); // 도망 실패 후 일반 전투 진입
                    return; // 조우 이동 차단
                } // 도망 실패 조건 끝
                pendingEscape = true; // 성공한 판정의 방 이동 대기
                fight.interactable = false; // 성공한 도망 이후 전투 선택 차단
                observe.interactable = false; // 성공한 도망 이후 관찰 선택 차단
                persuade.interactable = false; // 성공한 도망 이후 회유 선택 차단
            } // 속도 판정 끝
            if (!travel.TryReturnToPreviousRoom()) // 성공한 판정의 실제 방 이동 검사
            { // 이동 실패 조건 시작
                description.text = "이동 실패 · 도망을 다시 선택하세요"; // 주사위 재사용 없는 이동 재시도 안내
                return; // 기존 조우 화면과 성공 판정 유지
            } // 이동 실패 조건 끝
            owner.EndEncounterAfterEscape(); // 성공한 이동 후 탐험 화면 복귀
        } // 도망 끝
        private void LateUpdate() // 입력 관리자 복구 후 선택 재설정
        { // 선택 복구 시작
            if (pendingDefaultSelection && EventSystem.current != null) // 대기 중인 선택과 입력 관리자 검사
            { // 선택 복구 조건 시작
                SelectDefault(); // 전투 버튼의 기본 선택 복구
            } // 선택 복구 조건 끝
        } // 선택 복구 끝
        public void SelectDefault() // 전투 실패 후 조우 선택 복구
        { // 선택 시작
            if (fight != null && EventSystem.current != null) // 선택 버튼과 입력 관리자 검사
            { // 선택 가능 조건 시작
                EventSystem.current.SetSelectedGameObject(fight.gameObject); // 조우 전투 선택 복구
                pendingDefaultSelection = false; // 완료된 선택 대기 해제
                return; // 선택 복구 완료
            } // 선택 가능 조건 끝
            pendingDefaultSelection = true; // 입력 관리자 복구 후 선택 예약
        } // 선택 끝
    } // 클래스 끝
} // 이름 공간 끝
