using System; // 화면 생성 오류 처리
using ProjectDelta.Core; // 시험 이벤트 전투 규칙
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 화면 선택과 취소 입력
using UnityEngine.UI; // 버튼과 화면 구성
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventBattleScreenView : MonoBehaviour, ICancelHandler // 시험 이벤트 전투 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차 관리자
        private Text status; // 주도권과 플레이어 정력
        private Text notice; // 저장 실패와 행동 안내
        private Text result; // 승패와 시험 경험치
        private readonly Button[] targets = new Button[3]; // 개별 상대 선택 버튼
        private readonly GameObject[] portraits = new GameObject[3]; // 개별 상대 초상화
        private readonly Button[] actions = new Button[4]; // 관찰과 시험 행동 버튼
        private Button confirm; // 전투 결과 확인 버튼
        private Button retry; // 상대 자동 행동의 저장 재시도 버튼
        private Button retreat; // 시험 전투 중단 요청 버튼
        private GameObject retreatDialog; // 보상 포기와 복귀의 최종 확인 화면
        private Text retreatNotice; // 중단 저장 실패 안내
        private Button cancelRetreat; // 안전한 기본 취소 버튼
        private bool advanceFailed; // 자동 행동 저장 실패 여부
        public bool IsRetreatConfirmationOpen => retreatDialog != null && retreatDialog.activeSelf; // 중단 확인 화면의 실제 표시 여부
        private int selectedTarget = -1; // 현재 선택한 상대 번호
        private bool busy; // 단일 행동 중복 처리 차단
        private bool initialSelection = true; // 저장 재개 후 최초 대상 복원 여부
        public static TrialEventBattleScreenView Create(AppRoot owner) // 저장된 이벤트 전투 화면 생성
        { // 생성 시작
            if (owner == null || owner.CurrentTrialEventBattle == null || EventSystem.current == null) // 전투 상태와 입력 관리자 검사
            { // 오류 시작
                throw new InvalidOperationException("Missing event battle state or input system."); // 조작 불가능한 화면 차단
            } // 오류 끝
            var root = new GameObject("TrialEventBattleScreen", typeof(TrialEventBattleScreenView)); // 전투 화면 상위 객체
            try // 화면 생성 보호
            { // 보호 시작
                var view = root.GetComponent<TrialEventBattleScreenView>(); // 전투 화면 제어
                view.owner = owner; // 현재 회차 연결
                view.Build(); // 실제 전투 화면 구성
                view.Refresh(); // 저장된 전투 상태 표시
                return view; // 생성된 전투 화면 전달
            } // 보호 끝
            catch // 화면 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build() // 시험 이벤트 전투 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("TrialEventBattleCanvas"); // 이벤트 전투 전용 화면
            canvas.transform.SetParent(transform, false); // 화면 상위 객체 연결
            canvas.sortingOrder = 115; // 일반 전투 화면 앞 표시
            var background = new GameObject("TrialEventBattleBackground", typeof(RectTransform), typeof(Image)); // 전체 화면 배경
            background.transform.SetParent(canvas.transform, false); // 배경의 화면 연결
            var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 전투 화면 바탕색
            var accent = new Color(0.70f, 0.87f, 0.80f, 1f); // 상태 강조 색상
            UiFactory.CreateText(canvas.transform, "TrialEventBattleTitle", "시험 이벤트 전투", 43, new Vector2(-650f, 460f), new Vector2(550f, 70f), Color.white); // 시험 전투 제목
            status = UiFactory.CreateText(canvas.transform, "TrialEventBattleStatus", "", 27, new Vector2(220f, 460f), new Vector2(1000f, 70f), accent); // 정력과 현재 주도권
            result = UiFactory.CreateText(canvas.transform, "TrialEventBattleResult", "", 35, new Vector2(0f, -35f), new Vector2(1300f, 100f), Color.white); // 전투 완료 결과
            notice = UiFactory.CreateText(canvas.transform, "TrialEventBattleNotice", "상대를 선택하고 행동하세요 · 진행량과 정력 비용은 시험값", 24, new Vector2(0f, -240f), new Vector2(1500f, 80f), Color.white); // 행동과 저장 결과 안내
            var room = GameObject.Find("RoomTravel"); // 실제 전투 방 객체
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 원래 전투 대상 조회
            var monsterId = travel == null ? string.Empty : travel.CurrentMonsterId; // 원래 일반 전투의 상대 종류
            var portraitPath = monsterId == "MON_GOBLIN" ? "BattlePortraits/GoblinPrototype" : monsterId == "MON_MIMIC" ? "BattlePortraits/MimicPrototype" : monsterId == "MON_SLIME" ? "BattlePortraits/SlimePrototype" : "BattlePortraits/SlimeQueenPrototype"; // 기존 몬스터 초상화
            for (var index = 0; index < targets.Length; index++) // 최대 세 상대 화면 구성
            { // 상대 구성 시작
                var entry = index; // 개별 상대의 배열 번호
                var participantCount = owner.CurrentTrialEventBattle.enemies.Length; // 실제 상대 수
                var x = (index - (participantCount - 1) / 2f) * 520f; // 상대 수에 맞는 중앙 정렬
                portraits[index] = AddPortrait(canvas.transform, "TrialEventBattlePortrait_" + index, portraitPath, new Vector2(x, 190f)); // 기존 상대 초상화 재사용
                targets[index] = MakeButton(canvas.transform, "TrialEventBattleTarget_" + index, "", new Vector2(x, -15f), new Vector2(470f, 100f), () => SelectTarget(entry)); // 개별 체력과 진행량 선택
            } // 상대 구성 끝
            AddPortrait(canvas.transform, "TrialEventBattlePlayerPortrait", "BattlePortraits/PlayerPrototype", new Vector2(-790f, -340f), new Vector2(180f, 180f)); // 기존 주인공 초상화 재사용
            for (var index = 0; index < actions.Length; index++) // 관찰과 세 시험 행동 구성
            { // 행동 구성 시작
                var action = index; // 실제 규칙 행동 번호
                var label = index == 0 ? "관찰 · 비용 0" : "행동 " + index + " · 비용 " + TrialEventBattleRules.TrialStaminaCost(index) + " · 진행 +" + TrialEventBattleRules.TrialProgress(index); // 실제 시험 비용과 진행량
                actions[index] = MakeButton(canvas.transform, "TrialEventBattleAction_" + index, label, new Vector2(-360f + index * 335f, -360f), new Vector2(310f, 100f), () => ChooseAction(action)); // 저장할 실제 행동 선택
            } // 행동 구성 끝
            confirm = MakeButton(canvas.transform, "TrialEventBattleConfirm", "결과 확인", new Vector2(0f, -360f), new Vector2(380f, 90f), Confirm); // 승패 결과 확인
            retry = MakeButton(canvas.transform, "TrialEventBattleRetry", "상대 행동 저장 재시도", new Vector2(0f, -125f), new Vector2(450f, 80f), RetryAdvance); // 실패한 상대 행동 저장 재시도
            retreat = MakeButton(canvas.transform, "TrialEventBattleRetreat", "시험 전투 중단", new Vector2(690f, -470f), new Vector2(390f, 75f), OpenRetreat); // 시험 전투 중단 요청
            BuildRetreatDialog(canvas.transform); // 보상 포기와 방 복귀 확인 구성
        } // 구성 끝
        private void BuildRetreatDialog(Transform parent) // 시험 중단의 별도 확인 화면 구성
        { // 구성 시작
            retreatDialog = new GameObject("TrialEventBattleRetreatDialog", typeof(RectTransform), typeof(Image)); // 확인 화면 배경
            retreatDialog.transform.SetParent(parent, false); // 전투 화면 위에 배치
            var rect = retreatDialog.GetComponent<RectTransform>(); // 확인 배경 위치
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            retreatDialog.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.98f); // 아래 전투 화면 입력 차단
            UiFactory.CreateText(retreatDialog.transform, "TrialEventBattleRetreatQuestion", "시험 전투를 중단하고 이전 방으로 돌아갑니다.\n이번 전투의 보상을 포기합니다.\n정식 도주 판정은 적용되지 않습니다.", 32, new Vector2(0f, 150f), new Vector2(1450f, 250f), Color.white); // 중단 결과와 시험 범위 안내
            retreatNotice = UiFactory.CreateText(retreatDialog.transform, "TrialEventBattleRetreatNotice", "", 24, new Vector2(0f, -20f), new Vector2(1400f, 70f), Color.white); // 중단 저장 오류 표시
            MakeButton(retreatDialog.transform, "TrialEventBattleConfirmRetreat", "중단", new Vector2(-220f, -150f), new Vector2(380f, 84f), ConfirmRetreat); // 손실을 확인한 중단
            cancelRetreat = MakeButton(retreatDialog.transform, "TrialEventBattleCancelRetreat", "취소", new Vector2(220f, -150f), new Vector2(380f, 84f), CancelRetreat); // 안전한 기본 취소
            retreatDialog.SetActive(false); // 초기 확인 화면 숨김
        } // 구성 끝
        public void ShowAdvanceFailure(string message) // 자동 상대 행동 저장 실패와 재시도 표시
        { // 실패 안내 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 전투 저장 상태
            if (battle == null || battle.outcome != 0 || battle.turn < 0) // 실제 상대 주도권 검사
            { // 표시 거절 시작
                return; // 종료 또는 플레이어 주도권의 재시도 차단
            } // 표시 거절 끝
            advanceFailed = true; // 저장 실패 재시도 대기
            notice.text = string.IsNullOrEmpty(message) ? "상대 행동을 저장하지 못했습니다. 다시 시도하세요." : message; // 실제 저장 실패 안내
            Refresh(); // 재시도 버튼과 입력 표시
        } // 실패 안내 끝
        private static GameObject AddPortrait(Transform parent, string name, string path, Vector2 position, Vector2? maximum = null) // 원본 이미지 비율을 보존하는 초상화
        { // 생성 시작
            var portrait = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 초상화 표시 객체
            portrait.transform.SetParent(parent, false); // 현재 전투 화면 연결
            var rect = portrait.GetComponent<RectTransform>(); // 초상화 위치와 크기
            rect.anchoredPosition = position; // 실제 상대 표시 위치
            var image = portrait.GetComponent<RawImage>(); // 초상화 이미지 구성
            image.texture = Resources.Load<Texture2D>(path); // 기존 참가자 이미지 조회
            image.raycastTarget = false; // 초상화의 버튼 입력 가림 방지
            var bounds = maximum ?? new Vector2(340f, 340f); // 화면 안의 최대 그림 크기
            if (image.texture == null || image.texture.width <= 0 || image.texture.height <= 0) // 이미지 누락과 크기 검사
            { // 이미지 누락 시작
                image.enabled = false; // 없는 이미지의 사각형 표시 차단
                rect.sizeDelta = bounds; // 화면 자리 유지
                return portrait; // 입력 가능한 전투 화면 유지
            } // 이미지 누락 끝
            var scale = Mathf.Min(bounds.x / image.texture.width, bounds.y / image.texture.height); // 원본 비율을 보존하는 축소 비율
            rect.sizeDelta = new Vector2(image.texture.width * scale, image.texture.height * scale); // 왜곡 없는 이미지 크기
            return portrait; // 생성된 이미지 객체 전달
        } // 생성 끝
        private Button MakeButton(Transform parent, string name, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action) // 취소 입력이 연결된 전투 버튼 생성
        { // 생성 시작
            var button = UiFactory.CreateButton(parent, name, text, position); // 공통 전투 버튼
            button.GetComponent<RectTransform>().sizeDelta = size; // 버튼 표시 크기
            var label = button.transform.Find("Label").GetComponent<Text>(); // 버튼의 상태 글자
            label.rectTransform.sizeDelta = size; // 상태 글자 영역
            label.fontSize = 22; // 상태 글자 크기
            label.resizeTextForBestFit = true; // 긴 상태의 버튼 영역 맞춤
            label.resizeTextMinSize = 14; // 상태 최소 글자 크기
            label.resizeTextMaxSize = 22; // 상태 최대 글자 크기
            button.onClick.AddListener(action); // 실제 대상 또는 행동 연결
            button.gameObject.AddComponent<TrialEventBattleCancelRelay>().Initialize(this); // 버튼의 취소 입력 연결
            return button; // 생성된 버튼 전달
        } // 생성 끝
        public void Refresh() // 저장된 전투 상태와 결과 표시
        { // 갱신 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 독립 전투 저장 상태
            if (battle == null || battle.enemies == null || battle.enemies.Length < 1 || battle.enemies.Length > targets.Length || battle.outcome < 0 || battle.outcome > 2 || battle.turn < -1 || battle.turn >= battle.enemies.Length) // 대상 수와 진행 상태 검사
            { // 상태 오류 시작
                throw new InvalidOperationException("Invalid event battle display state."); // 불완전한 전투 화면 차단
            } // 상태 오류 끝
            var running = battle.outcome == 0; // 미완료 전투 여부
            var playerTurn = running && battle.turn == -1; // 플레이어 행동 가능 여부
            if (!running || playerTurn) // 자동 재시도가 필요하지 않은 상태 검사
            { // 대기 해제 시작
                advanceFailed = false; // 이전 자동 저장 오류 해제
            } // 대기 해제 끝
            var dialogOpen = retreatDialog.activeSelf; // 중단 확인 화면 표시 여부
            if (initialSelection) // 최초 화면의 저장 대상 복원 검사
            { // 저장 대상 복원 시작
                initialSelection = false; // 이후 화면 갱신의 직접 선택 보존
                if (battle.steps != null) // 저장된 행동 기록 검사
                { // 기록 역순 탐색 시작
                    for (var index = battle.steps.Length - 1; index >= 0; index--) // 마지막 행동부터 기록 검사
                    { // 기록 검사 시작
                        var step = battle.steps[index]; // 현재 저장된 행동 기록
                        if (step == null || step.kind != 0) // 실제 행동 기록 검사
                        { // 비행동 기록 시작
                            continue; // 이전 행동 기록 검사
                        } // 비행동 기록 끝
                        if (step.target >= 0 && step.target < battle.enemies.Length && battle.enemies[step.target] != null && battle.enemies[step.target].status == 0) // 마지막 행동 대상의 현재 참가 검사
                        { // 대상 복원 시작
                            selectedTarget = step.target; // 마지막 실제 행동 대상 복원
                        } // 대상 복원 끝
                        break; // 마지막 행동 이후의 이전 기록 제외
                    } // 기록 검사 끝
                } // 기록 역순 탐색 끝
            } // 저장 대상 복원 끝
            if (selectedTarget < 0 || selectedTarget >= battle.enemies.Length || battle.enemies[selectedTarget] == null || battle.enemies[selectedTarget].status != 0) // 기존 대상의 참가 가능 여부 검사
            { // 대상 재선택 시작
                selectedTarget = -1; // 기존 대상 해제
                for (var index = 0; index < battle.enemies.Length; index++) // 저장된 상대 순회
                { // 대상 검사 시작
                    if (battle.enemies[index] != null && battle.enemies[index].status == 0) // 실제 참여 중인 상대 검사
                    { // 대상 선택 시작
                        selectedTarget = index; // 첫 참여 상대 지정
                        break; // 나머지 상대 검사 종료
                    } // 대상 선택 끝
                } // 대상 검사 끝
            } // 대상 재선택 끝
            var turnText = !running ? "전투 종료" : playerTurn ? "플레이어 주도권" : "상대 " + (battle.enemies[battle.turn].slot + 1) + " 주도권 · 자동 처리"; // 실제 전투와 주도권 상태
            status.text = turnText + " · 정력 " + battle.stamina + "/" + battle.initialStamina; // 실제 정력과 주도권 표시
            result.text = battle.outcome == 1 ? "승리 · 경험치 " + battle.experience : battle.outcome == 2 ? "패배 · 정력 소진" : string.Empty; // 저장된 승패와 경험치
            for (var index = 0; index < targets.Length; index++) // 모든 상대 표시 갱신
            { // 상대 표시 시작
                var visible = index < battle.enemies.Length; // 실제 참가자 존재 여부
                targets[index].gameObject.SetActive(visible && running); // 진행 중 실제 상대만 표시
                portraits[index].SetActive(visible); // 실제 참가자 초상화 표시
                if (!visible) // 없는 참가자 검사
                { // 누락 참가자 시작
                    continue; // 다음 상대 검사
                } // 누락 참가자 끝
                var enemy = battle.enemies[index]; // 저장된 개별 상대
                if (enemy == null) // 누락된 상대 검사
                { // 상태 오류 시작
                    throw new InvalidOperationException("Missing event battle participant."); // 불완전한 대상 표시 차단
                } // 상태 오류 끝
                var state = enemy.status == 0 ? "진행 중" : enemy.status == 1 ? "완료" : enemy.status == 2 ? "이탈" : "체력 소진"; // 실제 개별 상태
                targets[index].interactable = playerTurn && enemy.status == 0 && !dialogOpen; // 확인 중 대상 입력 차단
                targets[index].transform.Find("Label").GetComponent<Text>().text = (index == selectedTarget ? "[선택] " : "") + "상대 " + (enemy.slot + 1) + " · 체력 " + enemy.health + "\n진행 " + enemy.progress + "/100 · " + state; // 개별 체력과 진행 게이지
                var image = portraits[index].GetComponent<RawImage>(); // 현재 상대 초상화
                image.color = enemy.status == 0 ? Color.white : new Color(0.38f, 0.38f, 0.38f, 1f); // 종료된 상대의 흐린 표시
            } // 상대 표시 끝
            for (var index = 0; index < actions.Length; index++) // 관찰과 시험 행동 갱신
            { // 행동 갱신 시작
                actions[index].gameObject.SetActive(running); // 진행 중에만 행동 표시
                actions[index].interactable = playerTurn && selectedTarget >= 0 && !dialogOpen; // 확인 중 행동 입력 차단
            } // 행동 갱신 끝
            confirm.gameObject.SetActive(!running); // 종료 후에만 결과 확인 표시
            retry.gameObject.SetActive(running && !playerTurn && advanceFailed); // 상대 자동 저장 실패 때만 재시도 표시
            retry.interactable = !dialogOpen; // 확인 화면 중 재시도 차단
            retreat.gameObject.SetActive(running); // 진행 중에만 시험 중단 표시
            retreat.interactable = !dialogOpen; // 확인 화면 중 중복 중단 차단
            confirm.interactable = !dialogOpen; // 확인 화면 중 결과 처리 차단
            if (EventSystem.current != null) // 화면 입력 관리자 검사
            { // 기본 선택 시작
                var selected = dialogOpen ? cancelRetreat.gameObject : !running ? confirm.gameObject : playerTurn && selectedTarget >= 0 ? targets[selectedTarget].gameObject : advanceFailed ? retry.gameObject : retreat.gameObject; // 현재 단계의 안전한 기본 선택
                EventSystem.current.SetSelectedGameObject(selected); // 저장된 단계의 입력 대상 설정
            } // 기본 선택 끝
        } // 갱신 끝
        private void SelectTarget(int index) // 플레이어 주도권의 실제 대상 선택
        { // 선택 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 저장된 전투
            if (busy || retreatDialog.activeSelf || battle == null || battle.outcome != 0 || battle.turn != -1 || battle.enemies == null || index < 0 || index >= battle.enemies.Length || battle.enemies[index] == null || battle.enemies[index].status != 0) // 주도권과 참가 대상 검사
            { // 선택 거절 시작
                return; // 종료된 상대와 적 주도권 입력 차단
            } // 선택 거절 끝
            selectedTarget = index; // 실제 선택한 상대 번호
            Refresh(); // 개별 상태와 선택 표시 갱신
        } // 선택 끝
        private void ChooseAction(int action) // 선택한 상대의 관찰 또는 시험 행동
        { // 행동 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 저장된 전투
            if (busy || retreatDialog.activeSelf || battle == null || battle.outcome != 0 || battle.turn != -1 || battle.enemies == null || selectedTarget < 0 || selectedTarget >= battle.enemies.Length || battle.enemies[selectedTarget] == null || battle.enemies[selectedTarget].status != 0 || action < 0 || action >= actions.Length) // 실제 주도권과 대상 및 행동 검사
            { // 행동 거절 시작
                return; // 완료와 중복 및 잘못된 행동 차단
            } // 행동 거절 끝
            busy = true; // 저장 중 중복 입력 잠금
            try // 실제 행동과 원자적 저장 요청
            { // 요청 시작
                if (!owner.TryEventBattleAction(selectedTarget, action, out var message)) // 행동 저장 검사
                { // 오류 시작
                    notice.text = message; // 저장 오류와 재시도 안내
                } // 오류 끝
                else // 실제 행동 저장 성공
                { // 성공 시작
                    notice.text = message; // 실제 행동과 결과 안내
                } // 성공 끝
                Refresh(); // 저장된 주도권과 상태 표시
            } // 요청 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 행동 입력 허용
            } // 복구 끝
        } // 행동 끝
        private void RetryAdvance() // 저장에 실패한 상대 행동 재시도
        { // 재시도 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 저장된 전투
            if (busy || retreatDialog.activeSelf || !advanceFailed || battle == null || battle.outcome != 0 || battle.turn < 0) // 실제 실패와 상대 주도권 검사
            { // 재시도 거절 시작
                return; // 중복과 잘못된 상대 행동 차단
            } // 재시도 거절 끝
            busy = true; // 상대 행동 저장 중 중복 입력 잠금
            try // 실제 상대 행동 저장 재시도
            { // 재시도 처리 시작
                if (!owner.TryAdvanceEventBattle(out var message)) // 실제 상대 행동 저장 검사
                { // 저장 실패 시작
                    notice.text = message; // 실제 실패와 재시도 안내
                } // 저장 실패 끝
                else // 상대 행동 저장 성공
                { // 저장 성공 시작
                    advanceFailed = false; // 성공한 행동의 재시도 대기 해제
                    notice.text = message; // 실제 행동 결과 안내
                } // 저장 성공 끝
                Refresh(); // 저장된 주도권과 결과 표시
            } // 재시도 처리 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 입력 허용
            } // 복구 끝
        } // 재시도 끝
        private void OpenRetreat() // 시험 전투 중단의 최종 확인 열기
        { // 확인 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 저장된 전투
            if (busy || retreatDialog.activeSelf || battle == null || battle.outcome != 0) // 진행 상태와 중복 확인 검사
            { // 열기 거절 시작
                return; // 완료된 전투의 중단 차단
            } // 열기 거절 끝
            retreatNotice.text = string.Empty; // 이전 중단 오류 초기화
            retreatDialog.SetActive(true); // 보상 포기와 방 복귀 확인 표시
            Refresh(); // 아래 전투 버튼 잠금과 기본 취소 선택
        } // 확인 끝
        private void CancelRetreat() // 시험 중단 최종 확인 취소
        { // 취소 시작
            if (busy || !retreatDialog.activeSelf) // 실제 확인 단계 검사
            { // 취소 거절 시작
                return; // 중복 취소 차단
            } // 취소 거절 끝
            retreatDialog.SetActive(false); // 확인 화면 숨김
            Refresh(); // 기존 전투 상태와 입력 복구
        } // 취소 끝
        private void ConfirmRetreat() // 확인한 시험 전투 중단 요청
        { // 중단 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 저장된 전투
            if (busy || !retreatDialog.activeSelf || battle == null || battle.outcome != 0) // 확인 화면과 실제 진행 상태 검사
            { // 중단 거절 시작
                return; // 확인되지 않은 보상 포기 차단
            } // 중단 거절 끝
            busy = true; // 실제 중단 저장 중 중복 입력 잠금
            try // 보상 포기와 이전 방 복귀 요청
            { // 중단 처리 시작
                if (!owner.TryRetreatEventBattle(out var message)) // 실제 저장과 복귀 검사
                { // 실패 시작
                    retreatNotice.text = message; // 저장 실패와 재시도 안내
                } // 실패 끝
            } // 중단 처리 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 입력 허용
            } // 복구 끝
        } // 중단 끝
        private void Confirm() // 저장된 전투 결과 확인
        { // 확인 시작
            var battle = owner.CurrentTrialEventBattle; // 현재 저장된 전투
            if (busy || retreatDialog.activeSelf || battle == null || battle.outcome == 0) // 확인 화면과 종료 여부 검사
            { // 확인 거절 시작
                return; // 진행 중 결과 확인 차단
            } // 확인 거절 끝
            busy = true; // 완료 저장 중 중복 입력 잠금
            try // 실제 전투 결과의 완료 요청
            { // 요청 시작
                if (!owner.TryFinishEventBattle(out var message)) // 결과 저장과 복귀 검사
                { // 오류 시작
                    notice.text = message; // 저장 실패와 재시도 안내
                } // 오류 끝
            } // 요청 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 확인 입력 허용
            } // 복구 끝
        } // 확인 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 입력 차단
        { // 취소 시작
            eventData.Use(); // 다른 화면의 취소 입력 차단
            if (retreatDialog.activeSelf) // 중단 확인 화면 검사
            { // 확인 취소 시작
                CancelRetreat(); // 기본 취소로 기존 전투 복귀
                return; // 아래 전투 취소 안내 차단
            } // 확인 취소 끝
            var battle = owner.CurrentTrialEventBattle; // 현재 전투 상태
            notice.text = battle != null && battle.outcome != 0 ? "결과를 확인하세요." : "전투 중에는 취소할 수 없습니다."; // 단계별 이탈 차단 안내
        } // 취소 끝
    } // 화면 클래스 끝
    public sealed class TrialEventBattleCancelRelay : MonoBehaviour, ICancelHandler // 전투 버튼의 취소 입력 중계
    { // 클래스 시작
        private TrialEventBattleScreenView screen; // 연결된 이벤트 전투 화면
        public void Initialize(TrialEventBattleScreenView target) // 전투 화면 연결
        { // 연결 시작
            screen = target; // 대상 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 취소 입력 전달
        { // 전달 시작
            if (screen != null) // 연결된 화면 검사
            { // 전달 조건 시작
                screen.OnCancel(eventData); // 이벤트 전투에 취소 입력 전달
            } // 전달 조건 끝
        } // 전달 끝
    } // 클래스 끝
} // 이름 공간 끝
