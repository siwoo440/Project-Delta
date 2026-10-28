using ProjectDelta.Core; // 전투 라운드와 참가자
using UnityEngine; // 실제 화면 객체
using UnityEngine.EventSystems; // 버튼 기본 선택
using UnityEngine.UI; // 화면 배경과 버튼
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class CombatScreenView : MonoBehaviour // 시험 전투 화면
    { // 클래스 시작
        internal const int TrialPlayerSpeed = 10; // 시험 플레이어 속도
        internal const int TrialEnemySpeed = 5; // 시험 상대 속도
        internal const int TrialEnemyHealth = 70; // 시험 상대 최대 체력
        private readonly System.Random random = new System.Random(); // 전투 판정 난수
        private AppRoot owner; // 탐험 상태와 체력 관리자
        private BattleRound round; // 시험 전투 라운드 상태
        private BattleActor[] participants; // 도주 확률 계산용 참가자
        private BattleActor current; // 현재 시험 행동자
        private CombatFighter player; // 플레이어 시험 전투 능력치
        private CombatFighter enemy; // 상대 시험 전투 능력치
        private Text roundText; // 라운드와 행동자 표시
        private Text playerHealth; // 플레이어 체력 표시
        private Text enemyHealth; // 상대 체력 표시
        private Text notice; // 최근 행동 결과 표시
        private Button advance; // 기본 공격 버튼
        private Button flee; // 정식 도주 버튼
        private Button defend; // 방어 행동 버튼
        private Button exit; // 시험 종료 또는 결과 복귀 버튼
        private CombatOutcome outcome; // 현재 전투 승패 상태
        private int lastActionFrame = -1; // 같은 프레임의 중복 행동 차단
        public static System.Func<int, int, int> RandomOverride // 자동 검증용 난수 지정
        { // 속성 시작
            get; // 지정 난수 조회
            set; // 검증 난수 설정
        } // 속성 끝
        public static CombatScreenView Create(AppRoot owner) // 실제 시험 전투 화면 생성
        { // 생성 시작
            var screen = new GameObject("BattleScreen", typeof(CombatScreenView)); // 전투 화면 상위 객체
            try // 실제 화면 구성
            { // 화면 구성 시작
                var view = screen.GetComponent<CombatScreenView>(); // 새 전투 화면 제어
                view.owner = owner; // 탐험 상태 관리자 연결
                var canvas = UiFactory.CreateCanvas("BattleCanvas"); // 전투 전용 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 화면 상위 객체에 연결
                canvas.sortingOrder = 100; // 탐험 화면보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 전투 배경 객체
                background.transform.SetParent(canvas.transform, false); // 전투 화면 아래 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치 조회
                rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 화면 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
                rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 전투 화면 배경색
                var accent = new Color(0.62f, 0.87f, 0.79f); // 전투 안내 강조 색상
                AddPortrait(canvas.transform, "PlayerPortrait", "BattlePortraits/PlayerPrototype", new Vector2(-600f, 45f)); // 주인공 임시 초상화
                AddPortrait(canvas.transform, "EnemyPortrait", "BattlePortraits/SlimeQueenPrototype", new Vector2(600f, 45f)); // 상위 개체 임시 초상화
                UiFactory.CreateText(canvas.transform, "Header", "전투", 58, new Vector2(0f, 410f), new Vector2(1000f, 90f), Color.white); // 전투 화면 제목
                UiFactory.CreateText(canvas.transform, "Status", "조우 · 공격과 방어, 도주", 26, new Vector2(0f, 330f), new Vector2(1100f, 60f), accent); // 현재 전투 상태
                view.roundText = UiFactory.CreateText(canvas.transform, "RoundStatus", "", 27, new Vector2(0f, 245f), new Vector2(1100f, 60f), Color.white); // 라운드와 행동자 표시
                UiFactory.CreateText(canvas.transform, "Player", "플레이어", 38, new Vector2(-260f, 70f), new Vector2(300f, 100f), accent); // 플레이어 영역
                UiFactory.CreateText(canvas.transform, "Opponent", "상위 개체", 38, new Vector2(260f, 70f), new Vector2(300f, 100f), new Color(1f, 0.62f, 0.58f)); // 상대 영역
                view.playerHealth = UiFactory.CreateText(canvas.transform, "PlayerHealth", "", 24, new Vector2(-260f, -20f), new Vector2(300f, 65f), Color.white); // 플레이어 체력
                view.enemyHealth = UiFactory.CreateText(canvas.transform, "EnemyHealth", "", 24, new Vector2(260f, -20f), new Vector2(300f, 65f), Color.white); // 상대 체력
                UiFactory.CreateText(canvas.transform, "Center", "VS", 44, Vector2.zero, new Vector2(200f, 100f), Color.white); // 대결 구분 표시
                view.notice = UiFactory.CreateText(canvas.transform, "Notice", "기본 공격을 선택하세요", 25, new Vector2(0f, -175f), new Vector2(1200f, 70f), Color.white); // 최근 행동 결과
                view.advance = UiFactory.CreateButton(canvas.transform, "AdvanceBattleButton", "공격", new Vector2(-470f, -275f)); // 기본 공격 버튼
                view.advance.onClick.AddListener(view.Advance); // 실제 공격 진행 연결
                view.flee = UiFactory.CreateButton(canvas.transform, "FleeBattleButton", "도주", new Vector2(0f, -275f)); // 정식 도주 버튼
                view.flee.onClick.AddListener(view.Flee); // 실제 도주 판정 연결
                view.defend = UiFactory.CreateButton(canvas.transform, "DefendBattleButton", "방어", new Vector2(470f, -275f)); // 방어 행동 버튼
                view.defend.onClick.AddListener(view.Defend); // 실제 방어 행동 연결
                view.exit = UiFactory.CreateButton(canvas.transform, "ExitBattleButton", "시험 종료", new Vector2(0f, -390f)); // 시험 종료와 결과 복귀 버튼
                view.exit.onClick.AddListener(view.Complete); // 실제 전투 종료 연결
                canvas.gameObject.AddComponent<CombatActionLayout>().Initialize(canvas.GetComponent<RectTransform>(), view.advance, view.flee, view.defend); // 화면 너비별 행동 버튼 배치
                view.StartTrialRound(); // 시험 참가자의 첫 라운드 시작
                EventSystem.current.SetSelectedGameObject(view.advance.gameObject); // 기본 공격 버튼 선택 지정
                return view; // 준비된 화면 전달
            } // 화면 구성 끝
            catch // 화면 준비 실패 정리
            { // 실패 정리 시작
                screen.SetActive(false); // 실패한 화면 즉시 숨김
                Destroy(screen); // 불완전한 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        internal static void AddPortrait(Transform parent, string name, string resourcePath, Vector2 position) // 선택적인 화면 초상화 생성
        { // 생성 시작
            var texture = Resources.Load<Texture2D>(resourcePath); // 전투 이미지 리소스 조회
            if (texture == null) // 이미지 누락 검사
            { // 리소스 누락 조건 시작
                return; // 기존 전투 화면 유지
            } // 리소스 누락 조건 끝
            var portrait = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 전투 초상화 객체
            portrait.transform.SetParent(parent, false); // 전투 화면 아래 배치
            var rect = portrait.GetComponent<RectTransform>(); // 화면 위치 조회
            rect.anchorMin = new Vector2(0.5f, 0.5f); // 화면 중심 기준점
            rect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중심 기준점 고정
            var image = portrait.GetComponent<RawImage>(); // 초상화 이미지 표시
            image.texture = texture; // 실제 그림 연결
            image.raycastTarget = false; // 버튼 입력 가로채기 방지
            portrait.AddComponent<CombatPortraitLayout>().Initialize(parent.GetComponent<RectTransform>(), position.x < 0f, position.y); // 화면 너비에 맞는 초상화 배치
        } // 생성 끝
        private void StartTrialRound() // 시험 참가자의 첫 행동 준비
        { // 준비 시작
            participants = new[] // 시험 참가자 목록
            { // 목록 시작
                new BattleActor("플레이어", TrialPlayerSpeed, true, 0), // 시험 플레이어 속도
                new BattleActor("상위 개체", TrialEnemySpeed, false, 1) // 시험 상대 속도
            }; // 목록 끝
            player = new CombatFighter(participants[0], 100, 50, 40, 90, 40, owner.PlayerHealth); // 기존 기본 플레이어 능력치
            enemy = new CombatFighter(participants[1], TrialEnemyHealth, 11, 7, 85, 8); // 시험 상위 개체 능력치
            round = new BattleRound(participants); // 참가자별 라운드 생성
            if (!round.TryStartRound() || !round.TryNext(out current)) // 첫 라운드와 행동자 검사
            { // 준비 실패 조건 시작
                throw new System.InvalidOperationException("Trial round could not start."); // 잘못된 전투 화면 차단
            } // 준비 실패 조건 끝
            UpdateDisplay(); // 첫 행동자와 체력 표시
        } // 준비 끝
        private void Advance() // 플레이어 기본 공격과 적 기본 행동
        { // 행동 시작
            if (outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 전투 상태와 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 중복 또는 잘못된 행동 차단
            } // 행동 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 재요청 차단
            notice.text = string.Empty; // 새 라운드의 이전 공격 결과 초기화
            if (!ResolveAttack(player, enemy)) // 유효한 기본 공격 검사
            { // 공격 실패 조건 시작
                return; // 잘못된 공격의 라운드 진행 차단
            } // 공격 실패 조건 끝
            if (FinishIfNeeded()) // 플레이어 공격 후 승패 검사
            { // 전투 종료 조건 시작
                return; // 종료된 전투의 추가 행동 차단
            } // 전투 종료 조건 끝
            CompletePlayerTurn(); // 플레이어 행동 후 남은 순서 진행
        } // 행동 끝
        private void Defend() // 플레이어의 방어 행동
        { // 방어 시작
            if (outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 전투 상태와 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 중복 또는 잘못된 방어 차단
            } // 행동 거절 조건 끝
            if (!player.BeginDefense()) // 생존 참가자의 방어 시작 검사
            { // 방어 거절 조건 시작
                return; // 잘못된 방어 행동 차단
            } // 방어 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 재요청 차단
            notice.text = "방어 · 피해 감소 " + CombatRules.DefenseReduction(player.Defense) + "%"; // 방어 행동과 감소율 표시
            CompletePlayerTurn(); // 방어 행동 소모와 상대 행동
        } // 방어 끝
        private void Flee() // 정식 도주 시도
        { // 도주 시작
            if (outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 전투 상태와 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 잘못된 도주 차단
            } // 행동 거절 조건 끝
            var roll = Roll(0, 100); // 기존 도주 난수
            if (!FleeRules.TryFlee(current, participants, roll, out var escaped)) // 도주 판정 유효성 검사
            { // 도주 거절 조건 시작
                return; // 잘못된 도주 행동 차단
            } // 도주 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 재요청 차단
            if (escaped) // 도주 성공 검사
            { // 도주 성공 처리 시작
                var travelObject = GameObject.Find("RoomTravel"); // 현재 방 이동 객체 조회
                var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 현재 방 이동 제어
                travel?.CompleteCurrentEncounter(); // 기존 규칙의 조우 완료와 대상 제거
                owner.EndCombat(); // 보상 없이 탐험 복귀
                return; // 종료한 전투의 추가 행동 차단
            } // 도주 성공 처리 끝
            notice.text = "도주 실패 · 성공률 " + FleeRules.Chance(current, participants) + "%"; // 실제 실패와 확률 안내
            CompletePlayerTurn(); // 실패한 행동 소모와 상대 행동
        } // 도주 끝
        private void CompletePlayerTurn() // 플레이어 행동 후 적 행동과 다음 라운드
        { // 턴 진행 시작
            round.TryCompleteAction(); // 플레이어 행동 완료
            if (round.TryNext(out current)) // 적 행동 순서 확인
            { // 적 행동 조건 시작
                if (!current.IsPlayer) // 상대의 새 행동 차례 검사
                { // 상대 방어 해제 시작
                    enemy.EndDefense(); // 상대의 이전 방어 상태 해제
                } // 상대 방어 해제 끝
                if (!current.IsPlayer && ResolveAttack(enemy, player)) // 적의 기본 공격 처리
                { // 적 공격 성공 조건 시작
                    owner.UpdatePlayerHealth(player.Health); // 공격 후 현재 탐험 체력 반영
                    if (FinishIfNeeded()) // 적 공격 후 승패 검사
                    { // 전투 종료 조건 시작
                        return; // 종료된 전투의 추가 행동 차단
                    } // 전투 종료 조건 끝
                    round.TryCompleteAction(); // 적 행동 완료
                } // 적 공격 성공 조건 끝
            } // 적 행동 조건 끝
            if (!round.TryEndRound() || !round.TryStartRound() || !round.TryNext(out current)) // 다음 라운드 준비 검사
            { // 다음 라운드 실패 조건 시작
                roundText.text = "행동 가능한 참가자가 없습니다"; // 더 진행할 참가자 없음
                return; // 잘못된 행동자 표시 방지
            } // 다음 라운드 실패 조건 끝
            if (current.IsPlayer) // 새 플레이어 행동 차례 검사
            { // 플레이어 방어 해제 시작
                player.EndDefense(); // 이전 방어 상태 종료
            } // 플레이어 방어 해제 끝
            UpdateDisplay(); // 현재 라운드와 체력 갱신
        } // 행동 끝
        private bool ResolveAttack(CombatFighter attacker, CombatFighter defender) // 한 참가자의 기본 공격 처리
        { // 공격 시작
            var hitRoll = Roll(0, 100); // 기존 명중 난수
            var varianceRoll = Roll(0, 11); // 기존 피해 편차 난수
            if (!CombatRules.TryAttack(attacker, defender, hitRoll, varianceRoll, out var applied)) // 기본 공격 유효성 검사
            { // 공격 거절 조건 시작
                notice.text = "공격할 수 없습니다"; // 잘못된 행동 안내
                return false; // 잘못된 공격 차단
            } // 공격 거절 조건 끝
            var message = applied > 0 ? attacker.Actor.Name + " 공격 · " + applied + " 피해" : attacker.Actor.Name + " 공격 · 빗나감"; // 실제 공격 결과 문구
            notice.text = string.IsNullOrEmpty(notice.text) ? message : notice.text + "\n" + message; // 양쪽 공격 결과 함께 표시
            UpdateHealth(); // 체력 변화 즉시 표시
            return true; // 유효한 공격 처리 성공
        } // 공격 끝
        private int Roll(int minimum, int maximum) // 전투 전용 난수 조회
        { // 조회 시작
            return RandomOverride == null ? random.Next(minimum, maximum) : RandomOverride(minimum, maximum); // 실제 난수 또는 검증 난수
        } // 조회 끝
        private bool FinishIfNeeded() // 공격 후 승패 확인과 결과 표시
        { // 확인 시작
            outcome = CombatRules.Outcome(player, enemy); // 플레이어와 적 생존 상태 검사
            if (outcome == CombatOutcome.Ongoing) // 계속 진행할 전투 검사
            { // 진행 조건 시작
                return false; // 다음 행동 허용
            } // 진행 조건 끝
            roundText.text = outcome == CombatOutcome.Victory ? "승리" : "패배"; // 실제 전투 결과 표시
            advance.interactable = false; // 종료된 전투의 공격 차단
            flee.interactable = false; // 종료된 전투의 도주 차단
            defend.interactable = false; // 종료된 전투의 방어 차단
            exit.transform.Find("Label").GetComponent<Text>().text = outcome == CombatOutcome.Victory ? "탐험 복귀" : "타이틀로"; // 결과에 따른 복귀 안내
            EventSystem.current.SetSelectedGameObject(exit.gameObject); // 결과 확인 버튼 선택
            return true; // 전투 종료 확인
        } // 확인 끝
        private void Complete() // 시험 종료 또는 전투 결과 확정
        { // 완료 시작
            if (outcome == CombatOutcome.Defeat) // 플레이어 패배 검사
            { // 패배 조건 시작
                owner.ReturnToTitle(); // 패배 후 타이틀 복귀
                return; // 탐험 복귀 차단
            } // 패배 조건 끝
            if (outcome == CombatOutcome.Victory) // 실제 승리 검사
            { // 승리 조건 시작
                var travelObject = GameObject.Find("RoomTravel"); // 현재 방 이동 객체 조회
                var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 현재 방 이동 제어
                travel?.CompleteCurrentEncounter(); // 승리한 방의 조우 완료
            } // 승리 조건 끝
            owner.EndCombat(); // 보존된 탐험으로 복귀
        } // 완료 끝
        private void UpdateDisplay() // 현재 라운드와 체력 표시
        { // 표시 시작
            roundText.text = round.Number + "라운드 · " + current.Name + " 행동"; // 현재 행동 순서 표시
            UpdateHealth(); // 참가자 체력 갱신
        } // 표시 끝
        private void UpdateHealth() // 현재 참가자의 체력 표시
        { // 표시 시작
            playerHealth.text = "체력 " + player.Health + "/" + player.MaxHealth; // 플레이어 체력 표시
            enemyHealth.text = "체력 " + enemy.Health + "/" + enemy.MaxHealth; // 상대 체력 표시
        } // 표시 끝
    } // 클래스 끝
    public sealed class CombatActionLayout : MonoBehaviour // 화면 너비에 맞는 행동 버튼 배치
    { // 클래스 시작
        private RectTransform canvasRect; // 전투 화면의 실제 크기
        private Button attack; // 공격 버튼
        private Button flee; // 도주 버튼
        private Button defend; // 방어 버튼
        private float lastWidth = -1f; // 이전에 적용한 화면 너비
        public void Initialize(RectTransform canvas, Button attackButton, Button fleeButton, Button defendButton) // 화면과 행동 버튼 연결
        { // 연결 시작
            canvasRect = canvas; // 화면 크기 연결
            attack = attackButton; // 공격 버튼 연결
            flee = fleeButton; // 도주 버튼 연결
            defend = defendButton; // 방어 버튼 연결
            Refresh(canvasRect.rect.width); // 최초 화면 크기 반영
        } // 연결 끝
        private void LateUpdate() // 화면 너비 변경 감지
        { // 검사 시작
            if (canvasRect != null && !Mathf.Approximately(lastWidth, canvasRect.rect.width)) // 실제 화면 크기 변경 검사
            { // 크기 변경 조건 시작
                Refresh(canvasRect.rect.width); // 현재 화면에 맞게 버튼 배치
            } // 크기 변경 조건 끝
        } // 검사 끝
        public void Refresh(float canvasWidth) // 주어진 화면 너비의 버튼 크기와 위치
        { // 배치 시작
            lastWidth = canvasWidth; // 적용한 화면 너비 기록
            var width = Mathf.Min(320f, Mathf.Max(0f, (canvasWidth - 112f) / 3f)); // 양쪽 여백과 버튼 간격 확보
            var offset = width + 24f; // 버튼 사이 간격을 포함한 위치
            Place(attack, -offset, width); // 왼쪽 공격 버튼 배치
            Place(flee, 0f, width); // 중앙 도주 버튼 배치
            Place(defend, offset, width); // 오른쪽 방어 버튼 배치
        } // 배치 끝
        private static void Place(Button button, float x, float width) // 한 행동 버튼의 위치와 글자 영역
        { // 배치 시작
            var rect = button.GetComponent<RectTransform>(); // 실제 버튼 위치 조회
            rect.anchoredPosition = new Vector2(x, -275f); // 행동 버튼의 가로 위치
            rect.sizeDelta = new Vector2(width, 84f); // 화면 너비에 맞는 버튼 크기
            var label = button.transform.Find("Label").GetComponent<RectTransform>(); // 버튼 글자 영역 조회
            label.sizeDelta = rect.sizeDelta; // 버튼 안의 글자 영역 유지
        } // 배치 끝
    } // 클래스 끝
    public sealed class CombatPortraitLayout : MonoBehaviour // 화면 너비에 맞는 초상화 배치
    { // 클래스 시작
        private RectTransform canvasRect; // 전투 화면의 실제 크기
        private RectTransform portraitRect; // 초상화의 위치와 크기
        private RawImage portraitImage; // 초상화 이미지
        private float lastWidth = -1f; // 이전에 적용한 화면 너비
        private float sign; // 왼쪽 또는 오른쪽 방향
        private float vertical; // 초상화의 세로 위치
        public void Initialize(RectTransform canvas, bool left, float y) // 초상화와 기준 화면 연결
        { // 연결 시작
            canvasRect = canvas; // 전투 화면 연결
            portraitRect = GetComponent<RectTransform>(); // 초상화 위치 연결
            portraitImage = GetComponent<RawImage>(); // 초상화 이미지 연결
            sign = left ? -1f : 1f; // 참가자별 화면 방향
            vertical = y; // 초상화 세로 위치 보관
            Refresh(canvasRect.rect.width); // 최초 화면 크기 반영
        } // 연결 끝
        private void LateUpdate() // 창 크기 변경 감지
        { // 검사 시작
            if (canvasRect != null && !Mathf.Approximately(lastWidth, canvasRect.rect.width)) // 실제 화면 너비 변경 검사
            { // 크기 변경 조건 시작
                Refresh(canvasRect.rect.width); // 현재 너비에 맞게 재배치
            } // 크기 변경 조건 끝
        } // 검사 끝
        public void Refresh(float canvasWidth) // 주어진 화면 너비에 맞는 배치
        { // 배치 시작
            lastWidth = canvasWidth; // 적용한 화면 너비 기록
            var available = Mathf.Max(0f, canvasWidth * 0.5f - 436f); // 글자와 가장자리 사이 여유 너비
            var limit = Mathf.Min(360f, available); // 기준 크기와 실제 공간 중 작은 값
            portraitImage.enabled = limit > 0f; // 공간이 없을 때 이미지 숨김
            if (limit <= 0f) // 초상화 공간 부족 검사
            { // 공간 부족 조건 시작
                return; // 글자와 버튼 표시 보존
            } // 공간 부족 조건 끝
            var texture = portraitImage.texture; // 원본 초상화 조회
            var ratio = (float)texture.width / texture.height; // 원본 가로세로 비율
            var size = ratio >= 1f ? new Vector2(limit, limit / ratio) : new Vector2(limit * ratio, limit); // 비율을 유지한 표시 크기
            portraitRect.sizeDelta = size; // 초상화 표시 크기 반영
            portraitRect.anchoredPosition = new Vector2(sign * (420f + size.x * 0.5f), vertical); // 글자와 겹치지 않는 위치
        } // 배치 끝
    } // 클래스 끝
} // 이름 공간 끝
