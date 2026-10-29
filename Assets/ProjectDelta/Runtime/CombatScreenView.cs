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
        internal const int TrialEnemyHealth = RunRestoreRules.TrialEnemyMaxHealth; // 시험 상대 최대 체력
        private readonly System.Random random = new System.Random(); // 전투 판정 난수
        private AppRoot owner; // 탐험 상태와 체력 관리자
        private BattleRound round; // 시험 전투 라운드 상태
        private BattleActor[] participants; // 도주 확률 계산용 참가자
        private BattleActor current; // 현재 시험 행동자
        private CombatFighter player; // 플레이어 시험 전투 능력치
        private CombatFighter enemy; // 상대 시험 전투 능력치
        private CombatFighter[] groupFighters; // 시험 다수 적의 개별 전투 상태
        private MultiEnemyBattleState groupBattle; // 다수 적 선택과 승패 상태
        private Button[] groupTargets; // 개별 적 선택 버튼
        private Text roundText; // 라운드와 행동자 표시
        private Text playerHealth; // 플레이어 체력 표시
        private Text enemyHealth; // 상대 체력 표시
        private Text notice; // 최근 행동 결과 표시
        private Button advance; // 기본 공격 버튼
        private Button flee; // 정식 도주 버튼
        private Button defend; // 방어 행동 버튼
        private Button potion; // 시험 회복약 행동 버튼
        private Button skill; // 강한 일격 시험 기술 버튼
        private Button itemButton; // 선택한 전투 소비품 사용 버튼
        private GameObject itemDialog; // 전투 아이템 선택 화면
        private Text itemNotice; // 선택한 아이템의 사용 결과
        private Text playerMana; // 현재 플레이어 마나 표시
        private readonly SkillCooldownState skillCooldown = new SkillCooldownState(); // 기술의 자기 차례 대기 상태
        private Button exit; // 시험 종료 또는 결과 복귀 버튼
        private CombatOutcome outcome; // 현재 전투 승패 상태
        private int lastActionFrame = -1; // 같은 프레임의 중복 행동 차단
        private bool pendingRetreat; // 성공 후 안전한 방 이동을 기다리는 후퇴
        private BattleActor lastDamageSource; // 마지막 실제 피해를 입힌 상대
        private BattleActor lastInfluencer; // 마지막으로 영향을 준 상대
        private GameObject surrenderDialog; // 항복 확인 화면
        private bool surrenderConfirmed; // 실제 확인된 항복 여부
        private SavedBattleReward pendingVictoryReward; // 한 번 생성한 승리 보상 후보
        private bool confirmReplaceOtherRun; // 다른 회차 저장 교체의 명시적 확인 대기
        private int rewardEligibleEnemies; // 이번 전투 시작 시 생존한 보상 대상 수
        public static int? TrialEnemySpeedOverride // 검증용 상대 속도 지정
        { // 속성 시작
            get; // 지정 속도 조회
            set; // 검증 속도 설정
        } // 속성 끝
        public static System.Func<int, int, int> RandomOverride // 자동 검증용 난수 지정
        { // 속성 시작
            get; // 지정 난수 조회
            set; // 검증 난수 설정
        } // 속성 끝
        public static System.Func<int, int, int> RetreatRollOverride // 자동 검증용 후퇴 주사위 지정
        { // 속성 시작
            get; // 지정 주사위 조회
            set; // 검증 주사위 설정
        } // 속성 끝
        public static bool? TrialEquipmentDropOverride // 검증용 시험 장비 드롭 지정
        { // 속성 시작
            get; // 지정 여부 조회
            set; // 검증 설정 변경
        } // 속성 끝
        public static System.Func<int, int, int> RewardRollOverride // 자동 검증용 보상 주사위 지정
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
                AddBattlePanel(canvas.transform, "BattleTopPanel", new Vector2(0f, 480f), new Vector2(1920f, 120f)); // 상단 라운드와 유물 영역
                AddBattlePanel(canvas.transform, "BattlePlayerPanel", new Vector2(0f, -350f), new Vector2(1920f, 380f)); // 하단 플레이어와 행동 영역
                var travelObject = GameObject.Find("RoomTravel"); // 실제 던전 객체 조회
                var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 현재 조우 대상 조회
                var enemyName = travel == null ? "상위 개체" : travel.CurrentMonsterName; // 실제 전투 상대 이름
                var grouped = travel != null && travel.IsGroupEncounter; // 실제 다수 조우 여부
                var enemyImage = travel != null && travel.CurrentMonsterId == "MON_SLIME" ? "BattlePortraits/SlimePrototype" : travel != null && travel.CurrentMonsterId == "MON_GOBLIN" ? "BattlePortraits/GoblinPrototype" : "BattlePortraits/SlimeQueenPrototype"; // 실제 상대의 초상화
                var firstEnemyPanel = AddBattlePanel(canvas.transform, "EnemySlotPanel1", new Vector2(grouped ? -250f : 0f, 180f), new Vector2(420f, 410f)); // 첫 상대의 상단 슬롯 배경
                RectTransform secondEnemyPanel = null; // 선택적인 두 번째 적 슬롯
                if (grouped) // 두 번째 상대 슬롯 검사
                { // 추가 슬롯 시작
                    secondEnemyPanel = AddBattlePanel(canvas.transform, "EnemySlotPanel2", new Vector2(250f, 180f), new Vector2(420f, 410f)); // 두 번째 상대의 상단 슬롯 배경
                } // 추가 슬롯 끝
                AddBattlePortrait(canvas.transform, "PlayerPortrait", "BattlePortraits/PlayerPrototype", true, 0); // 아래쪽 주인공 초상화
                AddBattlePortrait(canvas.transform, "EnemyPortrait", enemyImage, false, grouped ? -1 : 0); // 위쪽 첫 상대 초상화
                if (grouped) // 두 번째 적 표시 검사
                { // 다수 초상화 시작
                    AddBattlePortrait(canvas.transform, "EnemyPortrait2", enemyImage, false, 1); // 위쪽 두 번째 상대 초상화
                } // 다수 초상화 끝
                UiFactory.CreateText(canvas.transform, "Header", "전투", 42, new Vector2(-730f, 480f), new Vector2(300f, 75f), Color.white); // 상단 전투 제목
                UiFactory.CreateText(canvas.transform, "RelicStatus", "유물 " + owner.Relics.Count + "/" + RelicState.Capacity + " · 공격+" + owner.Relics.AttackBonus + " 방어+" + owner.Relics.DefenseBonus + " 속도+" + owner.Relics.SpeedBonus, 20, new Vector2(700f, 480f), new Vector2(450f, 60f), accent); // 실제 보유 유물과 시험 효과 표시
                view.roundText = UiFactory.CreateText(canvas.transform, "RoundStatus", "", 27, new Vector2(0f, 480f), new Vector2(900f, 60f), Color.white); // 상단 라운드와 행동자 표시
                UiFactory.CreateText(canvas.transform, "Status", "적 행동 예고 · 준비 중", 21, new Vector2(0f, 405f), new Vector2(800f, 45f), accent); // 미구현 적 행동 예고 안내
                UiFactory.CreateText(canvas.transform, "Player", "플레이어", 30, new Vector2(-320f, -245f), new Vector2(300f, 70f), accent); // 하단 플레이어 이름
                var opponent = UiFactory.CreateText(canvas.transform, "Opponent", enemyName, 30, new Vector2(0f, -5f), new Vector2(400f, 55f), new Color(1f, 0.62f, 0.58f)); // 위쪽 단일 상대 이름
                view.playerHealth = UiFactory.CreateText(canvas.transform, "PlayerHealth", "", 24, new Vector2(0f, -245f), new Vector2(300f, 55f), Color.white); // 하단 플레이어 체력
                view.playerMana = UiFactory.CreateText(canvas.transform, "PlayerMana", "", 23, new Vector2(0f, -300f), new Vector2(300f, 50f), new Color(0.58f, 0.76f, 1f)); // 하단 플레이어 마나
                view.enemyHealth = UiFactory.CreateText(canvas.transform, "EnemyHealth", "", 24, new Vector2(0f, -55f), new Vector2(300f, 50f), Color.white); // 위쪽 단일 상대 체력
                if (travel != null && travel.IsGroupEncounter) // 시험 다수 조우 화면 검사
                { // 다수 대상 화면 시작
                    opponent.gameObject.SetActive(false); // 단일 대상 이름 숨김
                    view.enemyHealth.gameObject.SetActive(false); // 단일 대상 체력 숨김
                    view.groupTargets = new Button[2]; // 두 대상 선택 버튼 준비
                    for (var index = 0; index < view.groupTargets.Length; index++) // 실제 대상 버튼 순회
                    { // 대상 버튼 생성 시작
                        var slot = index; // 선택할 적 슬롯 보관
                        var target = UiFactory.CreateButton(canvas.transform, "GroupTarget" + (slot + 1), (travel.CurrentMonsterId == "MON_GOBLIN" ? "고블린 " : "슬라임 ") + (slot + 1), new Vector2(slot == 0 ? -250f : 250f, -20f)); // 위쪽 개별 적 선택 버튼
                        target.onClick.AddListener(() => view.SelectGroupTarget(slot)); // 해당 적 선택 연결
                        view.groupTargets[slot] = target; // 생성된 대상 버튼 보관
                    } // 대상 버튼 생성 끝
                } // 다수 대상 화면 끝
                canvas.gameObject.AddComponent<CombatEnemySlotsLayout>().Initialize(canvas.GetComponent<RectTransform>(), firstEnemyPanel, secondEnemyPanel, view.groupTargets); // 화면 너비별 적 슬롯과 선택 버튼
                view.notice = UiFactory.CreateText(canvas.transform, "Notice", "기본 공격을 선택하세요", 19, new Vector2(0f, -110f), new Vector2(1200f, 100f), Color.white); // 적과 플레이어 사이 행동 결과
                view.advance = UiFactory.CreateButton(canvas.transform, "AdvanceBattleButton", "공격", new Vector2(-470f, -275f)); // 기본 공격 버튼
                view.advance.onClick.AddListener(view.Advance); // 실제 공격 진행 연결
                view.flee = UiFactory.CreateButton(canvas.transform, "FleeBattleButton", "도주", new Vector2(0f, -275f)); // 정식 도주 버튼
                view.flee.onClick.AddListener(view.Flee); // 실제 도주 판정 연결
                view.defend = UiFactory.CreateButton(canvas.transform, "DefendBattleButton", "방어", new Vector2(470f, -275f)); // 방어 행동 버튼
                view.defend.onClick.AddListener(view.Defend); // 실제 방어 행동 연결
                view.potion = UiFactory.CreateButton(canvas.transform, "BattlePotionButton", "회복약", new Vector2(705f, -275f)); // 시험 회복약 사용 버튼
                view.potion.onClick.AddListener(view.UsePotion); // 전투 회복약 행동 연결
                view.skill = UiFactory.CreateButton(canvas.transform, "StrongStrikeButton", "강한 일격", new Vector2(0f, -275f)); // 시험 전투 기술 버튼
                view.skill.onClick.AddListener(view.StrongStrike); // 실제 기술 사용 연결
                var tempt = UiFactory.CreateButton(canvas.transform, "TemptBattleButton", "유혹 · 준비 중", Vector2.zero); // 미구현 유혹 행동
                var persuade = UiFactory.CreateButton(canvas.transform, "PersuadeBattleButton", "회유 · 준비 중", Vector2.zero); // 미구현 전투 회유 행동
                var observe = UiFactory.CreateButton(canvas.transform, "ObserveBattleButton", "관찰 · 준비 중", Vector2.zero); // 미구현 전투 관찰 행동
                view.itemButton = UiFactory.CreateButton(canvas.transform, "BattleItemListButton", "아이템 선택", Vector2.zero); // 전투 중 보유 아이템 선택
                view.itemButton.onClick.AddListener(view.OpenBattleItems); // 실제 전투 아이템 화면 연결
                tempt.interactable = false; // 미구현 유혹 차단
                persuade.interactable = false; // 미구현 전투 회유 차단
                observe.interactable = false; // 미구현 전투 관찰 차단
                view.exit = UiFactory.CreateButton(canvas.transform, "ExitBattleButton", "항복", Vector2.zero); // 하단 항복과 결과 복귀 버튼
                view.exit.onClick.AddListener(view.Complete); // 실제 전투 종료 연결
                view.surrenderDialog = new GameObject("SurrenderDialog", typeof(RectTransform), typeof(Image)); // 항복 확인 배경
                view.surrenderDialog.transform.SetParent(canvas.transform, false); // 전투 화면 위 배치
                view.surrenderDialog.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 320f); // 확인 화면 크기
                view.surrenderDialog.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.1f, 0.98f); // 확인 화면 배경색
                UiFactory.CreateText(view.surrenderDialog.transform, "Question", "항복하면 이번 회차가 종료됩니다", 30, new Vector2(0f, 85f), new Vector2(850f, 80f), Color.white); // 항복 결과 안내
                var yes = UiFactory.CreateButton(view.surrenderDialog.transform, "ConfirmSurrender", "항복", new Vector2(-205f, -45f)); // 항복 확정 버튼
                var no = UiFactory.CreateButton(view.surrenderDialog.transform, "CancelSurrender", "아니요", new Vector2(205f, -45f)); // 항복 취소 버튼
                yes.onClick.AddListener(view.ConfirmSurrender); // 실제 항복 확정 연결
                no.onClick.AddListener(view.CancelSurrender); // 항복 취소 연결
                view.surrenderDialog.SetActive(false); // 확인 화면 초기 숨김
                var actionLayout = canvas.gameObject.AddComponent<CombatActionLayout>(); // 화면 너비별 행동 배치
                actionLayout.Initialize(canvas.GetComponent<RectTransform>(), view.advance, view.flee, view.defend, view.potion, view.skill, -380f); // 하단 첫 행동 줄 배치
                actionLayout.SetSecondaryRow(new[] { tempt, persuade, observe, view.itemButton, view.exit }, -475f); // 아이템과 항복의 하단 두 번째 줄
                view.StartTrialRound(); // 시험 참가자의 첫 라운드 시작
                if (view.outcome == CombatOutcome.Ongoing) // 진행 중인 전투 검사
                { // 기본 선택 시작
                    EventSystem.current.SetSelectedGameObject(view.advance.gameObject); // 기본 공격 버튼 선택 지정
                } // 기본 선택 끝
                return view; // 준비된 화면 전달
            } // 화면 구성 끝
            catch // 화면 준비 실패 정리
            { // 실패 정리 시작
                screen.SetActive(false); // 실패한 화면 즉시 숨김
                Destroy(screen); // 불완전한 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private static RectTransform AddBattlePanel(Transform parent, string name, Vector2 position, Vector2 size) // 전투 영역의 비입력 배경 생성
        { // 배경 시작
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 화면 패널 객체
            panel.transform.SetParent(parent, false); // 전투 화면에 배치
            var rect = panel.GetComponent<RectTransform>(); // 패널 위치 조회
            rect.anchoredPosition = position; // 패널 중앙 위치
            rect.sizeDelta = size; // 패널 기준 크기
            var image = panel.GetComponent<Image>(); // 패널 배경 조회
            image.color = new Color(0.09f, 0.10f, 0.14f, 0.92f); // 기존 전투 화면의 진한 배경
            image.raycastTarget = false; // 버튼 입력 가로채기 방지
            return rect; // 생성한 패널 위치 전달
        } // 배경 끝
        private static void AddBattlePortrait(Transform parent, string name, string resourcePath, bool player, int slot) // 전투 전용 상하 초상화 생성
        { // 초상화 생성 시작
            var texture = Resources.Load<Texture2D>(resourcePath); // 실제 초상화 리소스
            if (texture == null) // 누락된 이미지 검사
            { // 누락 시작
                return; // 기존 전투 입력 유지
            } // 누락 끝
            var portrait = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 전투 초상화 객체
            portrait.transform.SetParent(parent, false); // 전투 화면에 배치
            var image = portrait.GetComponent<RawImage>(); // 실제 이미지 표시
            image.texture = texture; // 현재 참가자의 이미지
            image.raycastTarget = false; // 대상 선택 입력 가로채기 방지
            portrait.AddComponent<BattlePortraitLayout>().Initialize(parent.GetComponent<RectTransform>(), player, slot); // 상하 영역과 화면 너비 반영
        } // 초상화 생성 끝
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
            var travelObject = GameObject.Find("RoomTravel"); // 현재 방 이동 객체 조회
            var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 현재 방 이동 제어
            var enemyName = travel == null ? "상위 개체" : travel.CurrentMonsterName; // 실제 전투 상대 이름
            var grouped = travel != null && travel.IsGroupEncounter; // 현재 시험 다수 조우 여부
            var groupName = grouped && travel.CurrentMonsterId == "MON_GOBLIN" ? "고블린" : "슬라임"; // 현재 저장의 실제 다수 적 이름
            if (!owner.Equipment.TryCalculate(50 + owner.Relics.AttackBonus, 40 + owner.Relics.DefenseBonus, TrialPlayerSpeed + owner.Relics.SpeedBonus, 0, 150, 10, out var stats)) // 장비 보정을 적용한 능력치 검사
            { // 능력치 오류 시작
                throw new System.InvalidOperationException("Invalid equipment stats."); // 잘못된 장비의 전투 진입 차단
            } // 능력치 오류 끝
            participants = grouped ? new[] { new BattleActor("플레이어", stats.Speed, true, 0), new BattleActor(groupName + " 1", TrialEnemySpeedOverride ?? TrialEnemySpeed, false, 1), new BattleActor(groupName + " 2", TrialEnemySpeedOverride ?? TrialEnemySpeed, false, 2) } : new[] { new BattleActor("플레이어", stats.Speed, true, 0), new BattleActor(enemyName, TrialEnemySpeedOverride ?? TrialEnemySpeed, false, 1) }; // 실제 적 수와 장비 속도를 적용한 참가자
            player = new CombatFighter(participants[0], 100, stats.Attack, stats.Defense, 0, 40, owner.PlayerHealth); // 장비 공격력과 방어력 적용
            if (grouped) // 실제 시험 다수 조우 검사
            { // 다수 전투 준비 시작
                var health = travel.CurrentGroupEnemyHealth(); // 후퇴한 개별 적 체력 조회
                groupFighters = new[] { new CombatFighter(participants[1], TrialEnemyHealth, 11, 7, 0, 8, health[0]), new CombatFighter(participants[2], TrialEnemyHealth, 11, 7, 0, 8, health[1]) }; // 개별 적의 전투 체력 복원
                groupBattle = new MultiEnemyBattleState(participants[0], new[] { participants[1], participants[2] }); // 다수 대상 선택과 승패 연결
                notice.text = "공격할 " + groupName + "을 선택하세요"; // 명시적인 대상 선택 안내
            } // 다수 전투 준비 끝
            else // 기존 단일 전투 분기
            { // 단일 전투 준비 시작
                var enemyHealth = travel == null ? TrialEnemyHealth : travel.CurrentEncounterHealth(); // 후퇴 전 남은 시험 적 체력
                enemy = new CombatFighter(participants[1], TrialEnemyHealth, 11, 7, 0, 8, enemyHealth); // 시험 상대와 명중 보정 0
            } // 단일 전투 준비 끝
            if (groupFighters == null) // 단일 조우 검사
            { // 단일 보상 준비 시작
                rewardEligibleEnemies = enemy.Actor.IsAlive ? 1 : 0; // 이번 전투의 생존 상대 수
            } // 단일 보상 준비 끝
            else // 다수 적 분기
            { // 다수 보상 준비 시작
                foreach (var fighter in groupFighters) // 현재 전투의 적 순회
                { // 보상 대상 검사 시작
                    rewardEligibleEnemies += fighter.Actor.IsAlive ? 1 : 0; // 이전 후퇴에서 제거한 적 제외
                } // 보상 대상 검사 끝
            } // 다수 보상 준비 끝
            round = new BattleRound(participants); // 참가자별 라운드 생성
            if (!round.TryStartRound() || !round.TryNext(out current)) // 첫 라운드와 행동자 검사
            { // 준비 실패 조건 시작
                throw new System.InvalidOperationException("Trial round could not start."); // 잘못된 전투 화면 차단
            } // 준비 실패 조건 끝
            ContinueUntilPlayer(); // 빠른 상대 행동과 첫 플레이어 차례 처리
        } // 준비 끝
        private void Advance() // 플레이어 기본 공격과 적 기본 행동
        { // 행동 시작
            if (itemDialog != null || pendingRetreat || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 아이템 선택과 전투 상태 및 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 중복 또는 잘못된 행동 차단
            } // 행동 거절 조건 끝
            if (!HasSelectedTarget()) // 다수 전투의 생존 대상 검사
            { // 대상 누락 시작
                notice.text = "공격할 대상을 선택하세요"; // 직접 대상 선택 안내
                return; // 행동과 자원 보존
            } // 대상 누락 끝
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
            if (itemDialog != null || pendingRetreat || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 아이템 선택과 전투 상태 및 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 중복 또는 잘못된 방어 차단
            } // 행동 거절 조건 끝
            if (!player.BeginDefense()) // 생존 참가자의 방어 시작 검사
            { // 방어 거절 조건 시작
                return; // 잘못된 방어 행동 차단
            } // 방어 거절 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 재요청 차단
            notice.text = "방어 · 방어력 +10"; // 방어 행동의 적용 방어력 안내
            CompletePlayerTurn(); // 방어 행동 소모와 상대 행동
        } // 방어 끝
        private void StrongStrike() // 시험 기술 강한 일격 사용
        { // 기술 시작
            if (itemDialog != null || pendingRetreat || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 아이템 선택과 실제 전투 행동 검사
            { // 기술 거절 시작
                return; // 잘못된 기술 요청 차단
            } // 기술 거절 끝
            if (!HasSelectedTarget()) // 다수 전투의 생존 대상 검사
            { // 대상 누락 시작
                notice.text = "공격할 대상을 선택하세요"; // 직접 대상 선택 안내
                return; // 마나와 행동 차례 보존
            } // 대상 누락 끝
            var mana = owner.PlayerMana; // 현재 회차 마나 조회
            if (!skillCooldown.TryUse(ref mana, 10, 1)) // 마나 10과 대기 1턴의 사용 검사
            { // 사용 실패 시작
                notice.text = "마나가 부족하거나 기술이 대기 중입니다"; // 현재 행동을 보존하는 안내
                return; // 마나와 행동 차례 보존
            } // 사용 실패 끝
            lastActionFrame = Time.frameCount; // 같은 프레임 중복 행동 차단
            owner.UpdatePlayerMana(mana); // 실제 회차 마나 차감
            var hitRoll = Roll(0, 100); // 기술 명중 난수
            if (hitRoll < CombatRules.HitChance(player, enemy)) // 실제 대상의 명중 판정
            { // 명중 처리 시작
                var damage = CombatRules.Damage(player, enemy, 10); // 기획 시험 공격 보정 10
                var criticalRoll = Roll(0, 100); // 기술 치명타 난수
                var multiplier = owner.Equipment.Get(EquipmentSlot.Weapon)?.CriticalMultiplierPercent ?? 150; // 무기 또는 맨손 치명타 배율
                CriticalHitRules.TryApply(damage, 40, criticalRoll, multiplier, out var finalDamage, out var critical); // 기술 확률과 현재 무기 배율 적용
                var applied = enemy.ApplyDamage(finalDamage); // 실제 상대 체력 감소
                notice.text = "강한 일격" + (critical ? " · 치명타" : "") + " · " + applied + " 피해"; // 실제 기술 결과 표시
            } // 명중 처리 끝
            else // 빗나간 기술 분기
            { // 빗나감 처리 시작
                notice.text = "강한 일격 · 빗나감"; // 실패한 명중 안내
            } // 빗나감 처리 끝
            UpdateHealth(); // 실제 마나와 체력 표시 갱신
            if (FinishIfNeeded()) // 기술 사용 후 승패 검사
            { // 전투 종료 시작
                return; // 종료된 전투의 추가 행동 차단
            } // 전투 종료 끝
            CompletePlayerTurn(); // 기술 행동 소모와 상대 차례
        } // 기술 끝
        private void OpenBattleItems() // 전투 중 보유 소비품 선택
        { // 선택 시작
            if (itemDialog != null || surrenderDialog.activeSelf || owner.Flow.Phase != GamePhase.Combat || pendingRetreat || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 확인 화면과 실제 행동 상태 검사
            { // 선택 거절 시작
                return; // 잘못된 아이템 선택 차단
            } // 선택 거절 끝
            advance.interactable = false; // 선택 중 공격 차단
            flee.interactable = false; // 선택 중 도주 차단
            defend.interactable = false; // 선택 중 방어 차단
            potion.interactable = false; // 선택 중 바로 사용 차단
            skill.interactable = false; // 선택 중 기술 차단
            itemButton.interactable = false; // 중복 아이템 화면 차단
            exit.interactable = false; // 선택 중 항복 차단
            if (groupTargets != null) // 다수 대상 버튼 검사
            { // 대상 잠금 시작
                foreach (var target in groupTargets) // 실제 대상 버튼 순회
                { // 버튼 잠금 시작
                    target.interactable = false; // 선택 중 대상 변경 차단
                } // 버튼 잠금 끝
            } // 대상 잠금 끝
            var canvas = transform.Find("BattleCanvas"); // 실제 전투 화면 조회
            itemDialog = new GameObject("BattleItemDialog", typeof(RectTransform), typeof(Image)); // 전투 아이템 선택 배경
            itemDialog.transform.SetParent(canvas, false); // 전투 화면 위에 배치
            var rect = itemDialog.GetComponent<RectTransform>(); // 전체 화면 위치
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            itemDialog.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 0.98f); // 선택 화면 배경
            UiFactory.CreateText(itemDialog.transform, "BattleItemTitle", "사용할 아이템을 선택하세요", 40, new Vector2(0f, 390f), new Vector2(1200f, 80f), Color.white); // 아이템 선택 안내
            for (var index = 0; index < owner.Inventory.Slots.Count; index++) // 실제 소지품 슬롯 순회
            { // 슬롯 생성 시작
                var slotIndex = index; // 선택할 슬롯 번호
                var slot = owner.Inventory.Slots[index]; // 실제 소지품 묶음
                var rows = (owner.Inventory.Slots.Count + 1) / 2; // 실제 소지품의 두 열 행 수
                var spacing = Mathf.Min(85f, 370f / Mathf.Max(1, rows - 1)); // 안내문을 가리지 않는 슬롯 간격
                var button = UiFactory.CreateButton(itemDialog.transform, "BattleItemSlot_" + index, slot.IsEmpty ? "빈 슬롯" : slot.Name + " × " + slot.Quantity, new Vector2(index < rows ? -400f : 400f, 270f - (index % rows) * spacing)); // 개별 아이템 선택 버튼
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, Mathf.Min(65f, spacing - 3f)); // 실제 슬롯 버튼 크기
                button.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(700f, Mathf.Min(65f, spacing - 3f)); // 아이템 이름 표시 영역
                button.transform.Find("Label").GetComponent<Text>().fontSize = 24; // 아이템 이름 글자 크기
                button.interactable = TrialConsumableRules.Find(slot.ItemId) != null; // 지원하는 소비품만 선택 허용
                button.onClick.AddListener(() => UseBattleItem(slotIndex)); // 선택한 실제 슬롯 사용
                button.gameObject.AddComponent<BattleItemCancelRelay>().Initialize(this); // 슬롯 취소 입력 연결
            } // 슬롯 생성 끝
            itemNotice = UiFactory.CreateText(itemDialog.transform, "BattleItemNotice", "취소하면 행동을 소모하지 않습니다", 23, new Vector2(0f, -200f), new Vector2(1200f, 60f), Color.white); // 취소와 사용 결과 안내
            var cancel = UiFactory.CreateButton(itemDialog.transform, "CancelBattleItemButton", "취소", new Vector2(0f, -330f)); // 아이템 선택 취소 버튼
            cancel.onClick.AddListener(CloseBattleItems); // 기존 전투 복귀 연결
            cancel.gameObject.AddComponent<BattleItemCancelRelay>().Initialize(this); // 취소 버튼의 취소 입력 연결
            EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 기본 취소 선택
        } // 선택 끝
        private void UseBattleItem(int index) // 선택한 전투 소비품 사용
        { // 사용 시작
            if (itemDialog == null || owner.Flow.Phase != GamePhase.Combat || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount || index < 0 || index >= owner.Inventory.Slots.Count) // 선택 화면과 실제 행동 검사
            { // 사용 거절 시작
                return; // 잘못된 아이템 행동 차단
            } // 사용 거절 끝
            var item = TrialConsumableRules.Find(owner.Inventory.Slots[index].ItemId); // 실제 선택한 소비품
            if (item == null || !BattleConsumableRules.TryUse(owner.Inventory, index, player, owner.PlayerMana, 50, out var mana, out var restored)) // 실제 회복과 단일 소비 검사
            { // 사용 실패 시작
                itemNotice.text = "이 아이템을 사용할 수 없습니다"; // 최대 자원 또는 잘못된 사용 안내
                return; // 소지품과 행동 차례 보존
            } // 사용 실패 끝
            lastActionFrame = Time.frameCount; // 성공한 행동의 중복 사용 차단
            owner.UpdatePlayerMana(mana); // 확인된 마나 반영
            owner.UpdatePlayerHealth(player.Health); // 확인된 체력 반영
            CloseBattleItems(); // 선택 화면 닫기
            notice.text = item.Name + " · " + restored + " 회복"; // 실제 아이템 사용 결과
            UpdateHealth(); // 체력과 마나 표시 갱신
            CompletePlayerTurn(); // 성공한 사용의 행동 소모
        } // 사용 끝
        public void CloseBattleItems() // 전투 아이템 선택 취소 또는 종료
        { // 닫기 시작
            if (itemDialog == null) // 실제 선택 화면 검사
            { // 화면 없음 시작
                return; // 중복 닫기 차단
            } // 화면 없음 끝
            itemDialog.SetActive(false); // 기존 선택 화면 즉시 숨김
            Destroy(itemDialog); // 선택 화면 정리
            itemDialog = null; // 선택 화면 참조 해제
            advance.interactable = true; // 공격 선택 복구
            flee.interactable = true; // 도주 선택 복구
            defend.interactable = true; // 방어 선택 복구
            potion.interactable = true; // 바로 사용 복구
            skill.interactable = true; // 기술 선택 복구
            itemButton.interactable = true; // 아이템 선택 복구
            exit.interactable = true; // 항복 선택 복구
            RefreshGroupTargets(); // 생존한 적의 선택 복구
            EventSystem.current.SetSelectedGameObject(advance.gameObject); // 전투 기본 선택 복구
        } // 닫기 끝
        private void UsePotion() // 플레이어의 시험 회복약 사용 행동
        { // 회복약 사용 시작
            if (itemDialog != null || pendingRetreat || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 아이템 선택과 전투 상태 및 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 잘못된 회복약 행동 차단
            } // 행동 거절 조건 끝
            if (!BattlePotionRules.TryUse(owner.Inventory, player, AppRoot.TrialPotionId, AppRoot.TrialPotionRecovery, out var restored)) // 실제 회복과 회복약 소비 검사
            { // 사용 불가 조건 시작
                notice.text = "회복약이 없거나 사용할 수 없습니다"; // 아이템 부재 또는 회복 불가 안내
                return; // 아이템과 플레이어 행동 보존
            } // 사용 불가 조건 끝
            lastActionFrame = Time.frameCount; // 성공한 행동의 같은 프레임 재요청 차단
            owner.UpdatePlayerHealth(player.Health); // 회복된 전투 체력을 탐험 상태에 반영
            notice.text = "회복약 · 체력 " + restored + " 회복"; // 실제 회복 결과 표시
            UpdateHealth(); // 전투 체력 즉시 갱신
            CompletePlayerTurn(); // 회복 행동 소모와 상대 행동
        } // 회복약 사용 끝
        private void Flee() // 일반 전투의 속도 주사위 후퇴
        { // 후퇴 시작
            if (itemDialog != null || outcome != CombatOutcome.Ongoing || current == null || !current.IsPlayer || lastActionFrame == Time.frameCount) // 아이템 선택과 전투 상태 및 중복 행동 검사
            { // 행동 거절 조건 시작
                return; // 잘못된 후퇴 행동 차단
            } // 행동 거절 조건 끝
            var travelObject = GameObject.Find("RoomTravel"); // 현재 방 이동 객체 조회
            var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 현재 방 이동 제어
            if (travel == null || !travel.CanReturnToPreviousRoom()) // 직전 방과 도착 가능 여부 검사
            { // 이동 불가 조건 시작
                notice.text = "이전 방으로 이동할 수 없습니다"; // 안전하지 않은 후퇴 안내
                return; // 주사위와 기존 전투 보존
            } // 이동 불가 조건 끝
            lastActionFrame = Time.frameCount; // 같은 프레임의 재요청 차단
            if (!pendingRetreat) // 아직 판정하지 않은 후퇴 검사
            { // 후퇴 판정 시작
                var playerRoll = RetreatRollOverride == null ? random.Next(1, 21) : RetreatRollOverride(1, 21); // 플레이어의 후퇴 주사위
                var enemyRoll = RetreatRollOverride == null ? random.Next(1, 21) : RetreatRollOverride(1, 21); // 상대의 추격 주사위
                if (!CombatRetreatRules.TryResolve(current, participants, 0, 0, playerRoll, enemyRoll, out var escaped)) // 속도와 양쪽 주사위 검사
                { // 판정 오류 조건 시작
                    notice.text = "후퇴 판정을 처리할 수 없습니다"; // 잘못된 후퇴 판정 안내
                    return; // 전투와 행동 보존
                } // 판정 오류 조건 끝
                if (!escaped) // 후퇴 실패 검사
                { // 실패 조건 시작
                    notice.text = "후퇴 실패"; // 실패한 후퇴 안내
                    CompletePlayerTurn(); // 플레이어 행동 소모와 상대 행동
                    return; // 이전 방 이동 차단
                } // 실패 조건 끝
                pendingRetreat = true; // 성공 판정의 안전한 이동 대기
                advance.interactable = false; // 성공한 후퇴 이후 공격 차단
                defend.interactable = false; // 성공한 후퇴 이후 방어 차단
                potion.interactable = false; // 성공한 후퇴 이후 회복약 차단
                skill.interactable = false; // 성공한 후퇴 이후 기술 차단
                itemButton.interactable = false; // 성공한 후퇴 이후 아이템 선택 차단
                exit.interactable = false; // 성공한 후퇴 이후 시험 종료 차단
            } // 후퇴 판정 끝
            var stored = groupBattle == null ? travel.StoreCurrentEncounterHealth(enemy.Health) : travel.StoreCurrentGroupHealth(new[] { groupFighters[0].Health, groupFighters[1].Health }); // 단일 또는 다수 적의 개별 체력 보관
            if (!stored) // 현재 조우의 적 체력 보관 검사
            { // 체력 보관 실패 조건 시작
                notice.text = "적 체력을 보관할 수 없습니다 · 후퇴를 다시 선택하세요"; // 판정 보존과 재시도 안내
                return; // 현재 전투와 성공 판정 유지
            } // 체력 보관 실패 조건 끝
            if (!travel.TryReturnToPreviousRoom()) // 실제 직전 방의 안전한 이동 검사
            { // 이동 실패 조건 시작
                notice.text = "이동 실패 · 후퇴를 다시 선택하세요"; // 판정 보존과 재시도 안내
                return; // 현재 전투와 성공 판정 유지
            } // 이동 실패 조건 끝
            owner.EndCombat(); // 보상 없는 전투 종료와 탐험 복귀
        } // 후퇴 끝
        private void CompletePlayerTurn() // 플레이어 행동 후 적 행동과 다음 라운드
        { // 턴 진행 시작
            round.TryCompleteAction(); // 플레이어 행동 완료
            if (!SelectNextActor()) // 다음 행동자 준비 검사
            { // 다음 라운드 실패 조건 시작
                roundText.text = "행동 가능한 참가자가 없습니다"; // 더 진행할 참가자 없음
                return; // 잘못된 행동자 표시 방지
            } // 다음 라운드 실패 조건 끝
            ContinueUntilPlayer(); // 다음 플레이어 차례까지 상대 행동 처리
        } // 행동 끝
        private bool SelectNextActor() // 다음 행동자 또는 새 라운드 선택
        { // 선택 시작
            if (round.TryNext(out current)) // 현재 라운드의 다음 행동자 검사
            { // 기존 라운드 조건 시작
                return true; // 다음 행동자 선택 성공
            } // 기존 라운드 조건 끝
            return round.TryEndRound() && round.TryStartRound() && round.TryNext(out current); // 완료된 라운드 뒤 새 행동자 선택
        } // 선택 끝
        private void ContinueUntilPlayer() // 상대 자동 행동과 플레이어 차례 준비
        { // 처리 시작
            while (current != null && !current.IsPlayer) // 상대의 선행 행동 검사
            { // 상대 행동 시작
                var actingEnemy = FindGroupFighter(current) ?? enemy; // 실제 행동 중인 적 조회
                actingEnemy.EndDefense(); // 상대의 이전 방어 상태 해제
                if (!ResolveAttack(actingEnemy, player)) // 상대 기본 공격 검사
                { // 공격 실패 시작
                    return; // 잘못된 행동 진행 차단
                } // 공격 실패 끝
                owner.UpdatePlayerHealth(player.Health); // 공격 후 탐험 체력 반영
                if (FinishIfNeeded()) // 상대 공격 후 승패 검사
                { // 전투 종료 조건 시작
                    return; // 종료된 전투의 추가 행동 차단
                } // 전투 종료 조건 끝
                round.TryCompleteAction(); // 상대 행동 완료
                if (!SelectNextActor()) // 다음 행동자 선택 검사
                { // 선택 실패 시작
                    roundText.text = "행동 가능한 참가자가 없습니다"; // 행동 불가 안내
                    return; // 잘못된 행동자 표시 차단
                } // 선택 실패 끝
            } // 상대 행동 끝
            player.EndDefense(); // 플레이어의 이전 방어 상태 해제
            skillCooldown.BeginTurn(); // 새 플레이어 차례의 기술 대기 갱신
            UpdateDisplay(); // 현재 라운드와 체력 갱신
        } // 처리 끝
        private bool ResolveAttack(CombatFighter attacker, CombatFighter defender) // 한 참가자의 기본 공격 처리
        { // 공격 시작
            var hitRoll = Roll(0, 100); // 기존 명중 난수
            var weapon = attacker.Actor.IsPlayer ? owner.Equipment.Get(EquipmentSlot.Weapon) : null; // 플레이어의 실제 장착 무기
            var applied = 0; // 실제 피해량 초기화
            var critical = false; // 치명타 결과 초기화
            if (weapon == null) // 무기가 없는 기존 기본 공격 검사
            { // 기존 공격 시작
                if (!CombatRules.TryAttack(attacker, defender, hitRoll, out applied)) // 기존 명중과 생존 검사
                { // 공격 거절 조건 시작
                    notice.text = "공격할 수 없습니다"; // 잘못된 행동 안내
                    return false; // 잘못된 공격 차단
                } // 공격 거절 조건 끝
            } // 기존 공격 끝
            else if (attacker.Actor.IsAlive && defender != null && defender.Actor.IsAlive && attacker.Actor.IsPlayer != defender.Actor.IsPlayer) // 무기 공격의 생존과 대상 검사
            { // 무기 공격 시작
                if (hitRoll < CombatRules.HitChance(attacker, defender)) // 실제 명중 판정
                { // 명중 시작
                    var damage = CombatRules.Damage(attacker, defender); // 장비 능력치를 반영한 피해
                    CriticalHitRules.TryApply(damage, weapon.CriticalChance, Roll(0, 100), weapon.CriticalMultiplierPercent, out var finalDamage, out critical); // 무기별 확률과 배율 적용
                    applied = defender.ApplyDamage(finalDamage); // 최종 피해 적용
                } // 명중 끝
            } // 무기 공격 끝
            else // 잘못된 무기 공격 분기
            { // 공격 거절 시작
                notice.text = "공격할 수 없습니다"; // 잘못된 행동 안내
                return false; // 잘못된 공격 차단
            } // 공격 거절 끝
            var message = applied > 0 ? attacker.Actor.Name + " 공격" + (critical ? " · 치명타" : "") + " · " + applied + " 피해" : attacker.Actor.Name + " 공격 · 빗나감"; // 실제 공격 결과 문구
            if (!attacker.Actor.IsPlayer && applied > 0) // 상대의 실제 피해 검사
            { // 피해 원인 기록 시작
                lastDamageSource = attacker.Actor; // 마지막 직접 피해 상대
                lastInfluencer = attacker.Actor; // 마지막 영향 상대
            } // 피해 원인 기록 끝
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
            outcome = groupBattle == null ? CombatRules.Outcome(player, enemy) : groupBattle.Outcome; // 단일 또는 다수 적의 승패 검사
            if (groupBattle != null && groupBattle.SelectedEnemy == null) // 선택한 적 사망 여부 검사
            { // 대상 해제 시작
                enemy = null; // 사망한 대상의 자동 선택 해제
                RefreshGroupTargets(); // 생존 대상 표시 갱신
            } // 대상 해제 끝
            if (outcome == CombatOutcome.Ongoing) // 계속 진행할 전투 검사
            { // 진행 조건 시작
                return false; // 다음 행동 허용
            } // 진행 조건 끝
            roundText.text = outcome == CombatOutcome.Victory ? "승리" : "패배"; // 실제 전투 결과 표시
            advance.interactable = false; // 종료된 전투의 공격 차단
            flee.interactable = false; // 종료된 전투의 도주 차단
            defend.interactable = false; // 종료된 전투의 방어 차단
            potion.interactable = false; // 종료된 전투의 회복약 사용 차단
            skill.interactable = false; // 종료된 전투의 기술 차단
            itemButton.interactable = false; // 종료된 전투의 아이템 선택 차단
            exit.transform.Find("Label").GetComponent<Text>().text = outcome == CombatOutcome.Victory ? "탐험 복귀" : "타이틀로"; // 결과에 따른 복귀 안내
            EventSystem.current.SetSelectedGameObject(exit.gameObject); // 결과 확인 버튼 선택
            return true; // 전투 종료 확인
        } // 확인 끝
        private void Complete() // 시험 종료 또는 전투 결과 확정
        { // 완료 시작
            if (itemDialog != null) // 전투 아이템 선택 검사
            { // 선택 중 종료 차단 시작
                return; // 아이템 선택 중 다른 종료 차단
            } // 선택 중 종료 차단 끝
            if (pendingRetreat) // 성공한 후퇴의 이동 대기 검사
            { // 후퇴 대기 조건 시작
                return; // 확정된 후퇴 중 다른 전투 종료 차단
            } // 후퇴 대기 조건 끝
            if (outcome == CombatOutcome.Defeat) // 플레이어 패배 검사
            { // 패배 조건 시작
                FinishDefeat(); // 기록 확인 후 타이틀 복귀
                return; // 탐험 복귀 차단
            } // 패배 조건 끝
            if (outcome == CombatOutcome.Ongoing) // 진행 중인 전투 검사
            { // 항복 확인 시작
                surrenderDialog.SetActive(true); // 항복 확인 화면 표시
                advance.interactable = false; // 확인 중 공격 차단
                flee.interactable = false; // 확인 중 도주 차단
                defend.interactable = false; // 확인 중 방어 차단
                potion.interactable = false; // 확인 중 회복 차단
                skill.interactable = false; // 확인 중 기술 차단
                itemButton.interactable = false; // 확인 중 아이템 선택 차단
                exit.interactable = false; // 중복 확인 화면 차단
                if (groupTargets != null) // 다수 대상 버튼 검사
                { // 대상 잠금 시작
                    foreach (var target in groupTargets) // 실제 선택 버튼 순회
                    { // 버튼 잠금 시작
                        target.interactable = false; // 확인 중 대상 선택 차단
                    } // 버튼 잠금 끝
                } // 대상 잠금 끝
                EventSystem.current.SetSelectedGameObject(surrenderDialog.transform.Find("CancelSurrender").gameObject); // 기본 선택을 취소로 설정
                return; // 확인 전 전투 보존
            } // 항복 확인 끝
            if (outcome == CombatOutcome.Victory) // 실제 승리 검사
            { // 승리 조건 시작
                var travelObject = GameObject.Find("RoomTravel"); // 현재 방 이동 객체 조회
                var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 현재 방 이동 제어
                if (travel != null && travel.CanCompleteCurrentEncounter()) // 실제 미완료 조우 승리 검사
                { // 실제 보상 준비 시작
                    if (pendingVictoryReward == null) // 아직 생성하지 않은 후보 검사
                    { // 후보 생성 시작
                        var elite = travel.CurrentMonsterId == "trial-elite" ? 1 : 0; // 시험 상위 개체 수
                        var normal = elite == 0 ? rewardEligibleEnemies : 0; // 이번 전투 시작 시 생존한 일반 적 수
                        if (!ExperienceRules.TryCalculateVictory(travel.Floor, normal, elite, out var experience)) // 실제 적별 경험치 검사
                        { // 경험치 오류 시작
                            notice.text = "경험치를 계산할 수 없습니다."; // 실제 계산 오류 안내
                            return; // 잘못된 보상 차단
                        } // 경험치 오류 끝
                        var potions = 0; // 실제 드롭 후보 수
                        for (var index = 0; index < normal + elite; index++) // 승리한 실제 적 순회
                        { // 개별 드롭 시작
                            var roll = RewardRollOverride == null ? random.Next(0, 100) : RewardRollOverride(0, 100); // 보상 전용 난수
                            if (roll < (elite > 0 ? 70 : 30)) // 기획의 개별 소비 아이템 확률
                            { // 아이템 후보 시작
                                potions++; // 시험 회복약 후보 추가
                            } // 아이템 후보 끝
                        } // 개별 드롭 끝
                        var itemId = string.Empty; // 기본 소비품 후보 없음
                        if (potions > 0) // 실제 소비품 후보 존재 검사
                        { // 종류 선택 시작
                            var kinds = new[] { "trial-small-potion", "trial-large-potion", "trial-mana-potion" }; // 현재 지원하는 시험 소비품 종류
                            var itemRoll = RewardRollOverride == null ? random.Next(0, kinds.Length) : RewardRollOverride(0, kinds.Length); // 시험 소비품 종류 선택
                            if (itemRoll < 0 || itemRoll >= kinds.Length) // 선택 범위 검사
                            { // 선택 오류 시작
                                notice.text = "보상 아이템을 결정할 수 없습니다."; // 잘못된 시험 난수 안내
                                return; // 잘못된 후보 저장 차단
                            } // 선택 오류 끝
                            itemId = kinds[itemRoll]; // 보관할 실제 소비품 종류
                        } // 종류 선택 끝
                        EquipmentVariantData equipment = null; // 기본 장비 후보 없음
                        if (potions == 0 && (TrialEquipmentDropOverride ?? random.Next(0, 100) < 10)) // 소비품이 없는 시험 장비 당첨 검사
                        { // 시험 장비 후보 시작
                            var grades = new[] { EquipmentVariantRules.Common, EquipmentVariantRules.Uncommon, EquipmentVariantRules.Rare, EquipmentVariantRules.Legendary }; // 시험용 균등 등급 후보
                            var grade = TrialEquipmentDropOverride == true ? EquipmentVariantRules.Rare : grades[random.Next(0, grades.Length)]; // 검증용 희귀 또는 임시 등급 추첨
                            var cursed = TrialEquipmentDropOverride == true || random.Next(0, 100) < 20; // 검증용 저주 또는 임시 저주 확률
                            equipment = TrialEquipmentDropOverride == true ? TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, grade, cursed) : TrialEquipmentTraits.CreateRandom(EquipmentCatalog.BasicSwordId, grade, cursed, random); // 저장할 고유 시험 검과 확정된 무작위 효과
                        } // 시험 장비 후보 끝
                        pendingVictoryReward = new SavedBattleReward { transactionId = System.Guid.NewGuid().ToString("N"), normalEnemies = normal, eliteEnemies = elite, experience = experience, gold = 10 * normal + 30 * elite, potionCount = potions, itemId = itemId, equipment = equipment }; // 고유 거래와 확정된 시험 보상 후보
                    } // 후보 생성 끝
                    if (!owner.TryStageVictoryReward(pendingVictoryReward, confirmReplaceOtherRun, out var error)) // 후보의 사전 저장 검사
                    { // 저장 실패 시작
                        confirmReplaceOtherRun = error.StartsWith("다른 회차의 저장이 있습니다.", System.StringComparison.Ordinal); // 다른 회차 교체 확인 대기
                        notice.text = error; // 저장 실패 또는 기존 회차 안내
                        if (confirmReplaceOtherRun) // 명시적 교체 선택 검사
                        { // 교체 선택 표시 시작
                            exit.transform.Find("Label").GetComponent<Text>().text = "기존 저장 교체 후 보상 준비"; // 별도 교체 선택 안내
                        } // 교체 선택 표시 끝
                        return; // 저장 전 보상 화면 차단
                    } // 저장 실패 끝
                    if (!owner.OpenRewardScreen()) // 저장된 후보 화면 표시 검사
                    { // 표시 실패 시작
                        notice.text = "보상 화면을 열지 못했습니다. 다시 선택하세요."; // 저장된 후보 재시도 안내
                    } // 표시 실패 끝
                    return; // 보상 화면에서 완료 처리
                } // 실제 보상 준비 끝
            } // 승리 조건 끝
            owner.EndCombat(); // 보존된 탐험으로 복귀
        } // 완료 끝
        private void CancelSurrender() // 항복 취소
        { // 취소 시작
            surrenderDialog.SetActive(false); // 확인 화면 닫기
            advance.interactable = true; // 공격 선택 복구
            flee.interactable = true; // 도주 선택 복구
            defend.interactable = true; // 방어 선택 복구
            potion.interactable = true; // 회복 선택 복구
            skill.interactable = true; // 기술 선택 복구
            itemButton.interactable = true; // 아이템 선택 복구
            exit.interactable = true; // 항복 선택 복구
            RefreshGroupTargets(); // 다수 적의 생존 선택 복구
            EventSystem.current.SetSelectedGameObject(advance.gameObject); // 기존 행동 선택 복귀
        } // 취소 끝
        private void ConfirmSurrender() // 항복 확정
        { // 확정 시작
            surrenderDialog.SetActive(false); // 확인 화면 닫기
            surrenderConfirmed = true; // 직접 확인한 항복 기록
            exit.interactable = true; // 기록 재시도 버튼 복구
            FinishDefeat(); // 기록 확인 후 회차 종료
        } // 확정 끝
        private void FinishDefeat() // 실제 패배 대상 기록과 회차 종료
        { // 종료 시작
            var enemies = new BattleActor[participants.Length - 1]; // 실제 적 참가자 목록
            System.Array.Copy(participants, 1, enemies, 0, enemies.Length); // 실제 전투 상대 복사
            var selected = enemy != null && enemy.Actor.IsAlive ? enemy.Actor : null; // 실제 선택된 생존 상대
            if (!BattleDefeatRules.TryCreate(player.Actor, enemies, surrenderConfirmed, selected, lastInfluencer, lastDamageSource, out var record)) // 확인된 원인과 상대 검사
            { // 대상 오류 시작
                surrenderConfirmed = false; // 실패한 항복 확인 해제
                notice.text = "패배 대상을 확인할 수 없습니다. 대상을 선택하세요."; // 명시적 대상 선택 안내
                if (player.Actor.IsAlive) // 항복 대상 선택 가능 여부 검사
                { // 행동 복구 시작
                    CancelSurrender(); // 대상 선택과 행동 복구
                } // 행동 복구 끝
                return; // 근거 없는 기록과 종료 차단
            } // 대상 오류 끝
            outcome = CombatOutcome.Defeat; // 확정된 패배 이후 전투 진행 차단
            roundText.text = "패배"; // 확정된 패배 표시
            advance.interactable = false; // 패배 후 공격 차단
            flee.interactable = false; // 패배 후 도주 차단
            defend.interactable = false; // 패배 후 방어 차단
            potion.interactable = false; // 패배 후 회복 차단
            skill.interactable = false; // 패배 후 기술 차단
            itemButton.interactable = false; // 패배 후 아이템 선택 차단
            exit.transform.Find("Label").GetComponent<Text>().text = "기록 후 타이틀로"; // 저장 실패 시 재시도 안내
            var travelObject = GameObject.Find("RoomTravel"); // 현재 조우 객체
            var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 실제 조우 상태
            var targetId = travel == null ? null : travel.CurrentMonsterId; // 실제 전투 상대 식별자
            if (!owner.TryRecordDefeat(targetId, record.Reason, out var error)) // 영구 기록과 회차 종료 검사
            { // 저장 오류 시작
                notice.text = error + " 다시 선택해 주세요."; // 원본 보존과 재시도 안내
                return; // 저장 실패 시 전투 유지
            } // 저장 오류 끝
            owner.ReturnToTitle(); // 검증된 패배 기록 후 타이틀 복귀
        } // 종료 끝
        private void UpdateDisplay() // 현재 라운드와 체력 표시
        { // 표시 시작
            roundText.text = round.Number + "라운드 · " + current.Name + " 행동"; // 현재 행동 순서 표시
            UpdateHealth(); // 참가자 체력 갱신
        } // 표시 끝
        private void UpdateHealth() // 현재 참가자의 체력 표시
        { // 표시 시작
            playerHealth.text = "체력 " + player.Health + "/" + player.MaxHealth; // 플레이어 체력 표시
            if (groupBattle == null) // 기존 단일 대상 검사
            { // 단일 체력 표시 시작
                enemyHealth.text = "체력 " + enemy.Health + "/" + enemy.MaxHealth; // 상대 체력 표시
            } // 단일 체력 표시 끝
            else // 실제 다수 적 분기
            { // 다수 체력 표시 시작
                RefreshGroupTargets(); // 개별 적의 체력과 생존 표시 갱신
            } // 다수 체력 표시 끝
            playerMana.text = "마나 " + owner.PlayerMana + "/50"; // 실제 회차 마나 표시
        } // 표시 끝
        private bool HasSelectedTarget() // 현재 공격할 생존 적 확인
        { // 검사 시작
            return enemy != null && enemy.Actor.IsAlive && (groupBattle == null || groupBattle.SelectedEnemy == enemy.Actor); // 단일 또는 직접 선택된 적 확인
        } // 검사 끝
        private CombatFighter FindGroupFighter(BattleActor actor) // 행동 중인 다수 적 조회
        { // 조회 시작
            if (groupFighters == null) // 다수 적 목록 검사
            { // 단일 전투 시작
                return null; // 기존 단일 대상 사용
            } // 단일 전투 끝
            foreach (var fighter in groupFighters) // 실제 적 목록 순회
            { // 적 검사 시작
                if (fighter.Actor == actor) // 실제 행동 참가자 비교
                { // 일치 시작
                    return fighter; // 일치한 적 전달
                } // 일치 끝
            } // 적 검사 끝
            return null; // 일치하는 적 없음
        } // 조회 끝
        private void SelectGroupTarget(int index) // 실제 생존 적을 명시적으로 선택
        { // 선택 시작
            if (itemDialog != null || groupBattle == null || index < 0 || index >= groupFighters.Length || !groupBattle.TrySelectEnemy(groupFighters[index].Actor)) // 아이템 선택과 대상 범위 및 생존 검사
            { // 선택 거절 시작
                return; // 잘못된 선택 차단
            } // 선택 거절 끝
            enemy = groupFighters[index]; // 실제 공격 대상 연결
            notice.text = enemy.Actor.Name + " 선택"; // 직접 선택한 대상 안내
            RefreshGroupTargets(); // 선택과 체력 표시 갱신
        } // 선택 끝
        private void RefreshGroupTargets() // 개별 적의 체력과 선택 상태 표시
        { // 표시 시작
            if (groupTargets == null || groupFighters == null) // 화면과 실제 적 준비 검사
            { // 표시 불가 시작
                return; // 준비되지 않은 대상 표시 차단
            } // 표시 불가 끝
            for (var index = 0; index < groupFighters.Length; index++) // 적별 화면 순회
            { // 적 표시 시작
                var fighter = groupFighters[index]; // 현재 표시할 적
                var button = groupTargets[index]; // 현재 적의 선택 버튼
                button.interactable = fighter.Actor.IsAlive && outcome == CombatOutcome.Ongoing; // 살아 있는 적만 선택 허용
                button.transform.Find("Label").GetComponent<Text>().text = fighter.Actor.Name + " · " + fighter.Health + "/" + fighter.MaxHealth + (groupBattle.SelectedEnemy == fighter.Actor ? " ✓" : ""); // 체력과 직접 선택 여부 표시
            } // 적 표시 끝
        } // 표시 끝
    } // 클래스 끝
    public sealed class BattleItemCancelRelay : MonoBehaviour, ICancelHandler // 전투 아이템 선택의 취소 입력 중계
    { // 클래스 시작
        private CombatScreenView screen; // 실제 전투 화면
        public void Initialize(CombatScreenView target) // 전투 화면 연결
        { // 연결 시작
            screen = target; // 취소할 전투 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // 취소 키 또는 게임패드 취소 입력
        { // 취소 시작
            eventData.Use(); // 다른 화면으로 취소 전파 차단
            if (screen != null) // 실제 전투 화면 검사
            { // 화면 닫기 시작
                screen.CloseBattleItems(); // 행동 소모 없이 선택 취소
            } // 화면 닫기 끝
        } // 취소 끝
    } // 클래스 끝
    public sealed class CombatActionLayout : MonoBehaviour // 화면 너비에 맞는 행동 버튼 배치
    { // 클래스 시작
        private RectTransform canvasRect; // 전투 화면의 실제 크기
        private Button attack; // 공격 버튼
        private Button flee; // 도주 버튼
        private Button defend; // 방어 버튼
        private Button fourth; // 선택적인 네 번째 행동 버튼
        private Button fifth; // 선택적인 다섯 번째 행동 버튼
        private float lastWidth = -1f; // 이전에 적용한 화면 너비
        private float vertical = -275f; // 화면별 행동 버튼 세로 위치
        private Button[] secondary; // 선택적인 하단 추가 행동 줄
        private float secondaryVertical; // 추가 행동 줄 세로 위치
        public void Initialize(RectTransform canvas, Button attackButton, Button fleeButton, Button defendButton, Button fourthButton = null, Button fifthButton = null, float y = -275f) // 화면과 행동 버튼 연결
        { // 연결 시작
            canvasRect = canvas; // 화면 크기 연결
            attack = attackButton; // 공격 버튼 연결
            flee = fleeButton; // 도주 버튼 연결
            defend = defendButton; // 방어 버튼 연결
            fourth = fourthButton; // 선택적인 네 번째 버튼 연결
            fifth = fifthButton; // 선택적인 다섯 번째 버튼 연결
            vertical = y; // 현재 화면의 행동 세로 위치
            Refresh(canvasRect.rect.width); // 최초 화면 크기 반영
        } // 연결 끝
        public void SetSecondaryRow(Button[] buttons, float y) // 전투 화면의 추가 행동 줄 연결
        { // 연결 시작
            secondary = buttons; // 실제 추가 행동 목록
            secondaryVertical = y; // 추가 행동의 세로 위치
            Refresh(canvasRect.rect.width); // 현재 화면에 두 줄 적용
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
            var count = fifth != null ? 5 : fourth == null ? 3 : 4; // 실제 행동 버튼 개수
            var width = Mathf.Min(320f, Mathf.Max(0f, (canvasWidth - 64f - 24f * (count - 1)) / count)); // 양쪽 여백과 버튼 간격 확보
            var offset = width + 24f; // 버튼 사이 간격을 포함한 위치
            var left = -(count - 1) * offset / 2f; // 전체 버튼의 왼쪽 시작 위치
            Place(attack, left, width); // 첫 번째 행동 버튼 배치
            Place(flee, left + offset, width); // 두 번째 행동 버튼 배치
            Place(defend, left + 2f * offset, width); // 세 번째 행동 버튼 배치
            if (fourth != null) // 네 번째 행동 제공 검사
            { // 추가 행동 조건 시작
                Place(fourth, left + 3f * offset, width); // 네 번째 행동 버튼 배치
            } // 추가 행동 조건 끝
            if (fifth != null) // 다섯 번째 행동 제공 검사
            { // 추가 기술 조건 시작
                Place(fifth, left + 4f * offset, width); // 다섯 번째 행동 버튼 배치
            } // 추가 기술 조건 끝
            if (secondary != null && secondary.Length > 0) // 추가 행동 줄 존재 검사
            { // 추가 줄 배치 시작
                var secondWidth = Mathf.Min(320f, Mathf.Max(0f, (canvasWidth - 64f - 24f * (secondary.Length - 1)) / secondary.Length)); // 양쪽 여백과 간격을 확보한 추가 버튼 너비
                var secondLeft = -(secondary.Length - 1) * (secondWidth + 24f) / 2f; // 추가 행동 줄의 왼쪽 위치
                for (var index = 0; index < secondary.Length; index++) // 실제 추가 행동 순회
                { // 추가 행동 배치 시작
                    PlaceAt(secondary[index], secondLeft + index * (secondWidth + 24f), secondWidth, secondaryVertical, 64f); // 두 번째 줄의 화면별 위치
                } // 추가 행동 배치 끝
            } // 추가 줄 배치 끝
        } // 배치 끝
        private void Place(Button button, float x, float width) // 한 행동 버튼의 위치와 글자 영역
        { // 배치 시작
            PlaceAt(button, x, width, vertical, secondary == null ? 84f : 64f); // 화면 유형에 맞는 행동 높이
        } // 배치 끝
        private static void PlaceAt(Button button, float x, float width, float y, float height) // 한 행동의 실제 위치와 크기
        { // 배치 시작
            var rect = button.GetComponent<RectTransform>(); // 실제 버튼 위치 조회
            rect.anchoredPosition = new Vector2(x, y); // 행동 버튼의 가로와 세로 위치
            rect.sizeDelta = new Vector2(width, height); // 화면 너비에 맞는 버튼 크기
            var label = button.transform.Find("Label").GetComponent<RectTransform>(); // 버튼 글자 영역 조회
            label.sizeDelta = rect.sizeDelta; // 버튼 안의 글자 영역 유지
        } // 배치 끝
    } // 클래스 끝
    public sealed class BattlePortraitLayout : MonoBehaviour // 전투 화면의 상하 초상화 배치
    { // 클래스 시작
        private RectTransform canvasRect; // 전투 화면 크기
        private RectTransform portraitRect; // 실제 초상화 위치
        private RawImage portraitImage; // 실제 초상화 이미지
        private bool player; // 플레이어 표시 여부
        private int slot; // 상대의 왼쪽 또는 오른쪽 슬롯
        private float lastWidth = -1f; // 이전 화면 너비
        public void Initialize(RectTransform canvas, bool isPlayer, int enemySlot) // 참가자와 화면 연결
        { // 연결 시작
            canvasRect = canvas; // 실제 화면 연결
            portraitRect = GetComponent<RectTransform>(); // 초상화 위치 연결
            portraitImage = GetComponent<RawImage>(); // 이미지 표시 연결
            player = isPlayer; // 플레이어 여부 보관
            slot = enemySlot; // 상대 슬롯 보관
            Refresh(canvasRect.rect.width); // 현재 화면에 최초 배치
        } // 연결 끝
        private void LateUpdate() // 화면 너비 변경 확인
        { // 확인 시작
            if (canvasRect != null && !Mathf.Approximately(lastWidth, canvasRect.rect.width)) // 실제 화면 변경 검사
            { // 화면 변경 시작
                Refresh(canvasRect.rect.width); // 현재 너비에 맞게 재배치
            } // 화면 변경 끝
        } // 확인 끝
        public void Refresh(float width) // 화면 너비에 맞는 초상화 크기와 위치
        { // 배치 시작
            lastWidth = width; // 적용한 화면 너비 기록
            var limit = player ? (width < 900f ? 0f : Mathf.Min(175f, Mathf.Max(0f, width * 0.18f))) : Mathf.Min(320f, Mathf.Max(0f, width * (slot == 0 ? 0.35f : 0.27f))); // 참가자별 최대 표시 크기
            var x = player ? -Mathf.Max(0f, width * 0.5f - 150f) : slot * Mathf.Min(250f, Mathf.Max(0f, (width - 64f) / 4f)); // 하단 주인공과 상단 적 위치
            portraitRect.anchoredPosition = new Vector2(x, player ? -260f : 210f); // 행동 버튼과 겹치지 않는 참가자별 상하 위치
            portraitImage.enabled = limit > 0f; // 표시 공간 여부 반영
            if (limit <= 0f) // 표시할 공간 검사
            { // 공간 없음 시작
                return; // 잘못된 이미지 크기 차단
            } // 공간 없음 끝
            var ratio = (float)portraitImage.texture.width / portraitImage.texture.height; // 원본 이미지 비율
            portraitRect.sizeDelta = ratio >= 1f ? new Vector2(limit, limit / ratio) : new Vector2(limit * ratio, limit); // 원본 비율 유지
        } // 배치 끝
    } // 클래스 끝
    public sealed class CombatEnemySlotsLayout : MonoBehaviour // 화면 너비에 맞는 적 슬롯 배치
    { // 클래스 시작
        private RectTransform canvasRect; // 실제 전투 화면 크기
        private RectTransform first; // 첫 번째 적 배경
        private RectTransform second; // 선택적인 두 번째 적 배경
        private Button[] targets; // 선택적인 적별 버튼
        private float lastWidth = -1f; // 이전 화면 너비
        public void Initialize(RectTransform canvas, RectTransform firstPanel, RectTransform secondPanel, Button[] groupTargets) // 실제 적 슬롯 연결
        { // 연결 시작
            canvasRect = canvas; // 전투 화면 연결
            first = firstPanel; // 첫 슬롯 연결
            second = secondPanel; // 두 번째 슬롯 연결
            targets = groupTargets; // 실제 적 선택 버튼 연결
            Refresh(canvas.rect.width); // 현재 화면에 최초 배치
        } // 연결 끝
        private void LateUpdate() // 화면 너비 변경 감지
        { // 검사 시작
            if (canvasRect != null && !Mathf.Approximately(lastWidth, canvasRect.rect.width)) // 실제 너비 변경 검사
            { // 변경 시작
                Refresh(canvasRect.rect.width); // 현재 화면에 맞게 재배치
            } // 변경 끝
        } // 검사 끝
        public void Refresh(float width) // 상대 슬롯과 대상 버튼의 화면별 크기
        { // 배치 시작
            lastWidth = width; // 현재 화면 너비 기록
            var grouped = second != null; // 다수 적 슬롯 여부
            var slotWidth = Mathf.Min(420f, Mathf.Max(0f, grouped ? (width - 96f) / 2f : width - 64f)); // 화면 안쪽의 슬롯 너비
            var offset = grouped ? Mathf.Min(250f, Mathf.Max(0f, (width - 64f) / 4f)) : 0f; // 좌우 슬롯 간격
            first.anchoredPosition = new Vector2(-offset, 180f); // 첫 슬롯 위치
            first.sizeDelta = new Vector2(slotWidth, 410f); // 첫 슬롯 크기
            if (grouped) // 두 번째 슬롯 검사
            { // 두 번째 슬롯 배치 시작
                second.anchoredPosition = new Vector2(offset, 180f); // 두 번째 슬롯 위치
                second.sizeDelta = new Vector2(slotWidth, 410f); // 두 번째 슬롯 크기
                for (var index = 0; index < targets.Length; index++) // 실제 적별 선택 버튼 순회
                { // 선택 버튼 배치 시작
                    var rect = targets[index].GetComponent<RectTransform>(); // 현재 버튼 위치
                    rect.anchoredPosition = new Vector2(index == 0 ? -offset : offset, -20f); // 대응하는 적 아래 배치
                    rect.sizeDelta = new Vector2(Mathf.Min(380f, slotWidth), 84f); // 슬롯 안의 버튼 크기
                    rect.Find("Label").GetComponent<RectTransform>().sizeDelta = rect.sizeDelta; // 버튼 안의 글자 영역
                } // 선택 버튼 배치 끝
            } // 두 번째 슬롯 배치 끝
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
