using System; // 화면 생성 오류 처리
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 화면 선택과 취소 입력
using UnityEngine.UI; // 버튼과 화면 구성
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TrialNpcScreenView : MonoBehaviour, ICancelHandler // 시험 상인의 대화 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차 관리자
        private Text dialogue; // 상인의 현재 대화
        private Text affinity; // 현재 영구 호감도와 단계
        private Button friendly; // 회차당 한 번의 시험 우호 선택
        private Text notice; // 상점 진입 결과 안내
        private Button leave; // 안전한 기본 종료 버튼
        private int dialogueIndex; // 시험 대화 순서
        private bool busy; // 중복 입력 차단
        public static TrialNpcScreenView Create(AppRoot owner) // 상인 대화 화면 생성
        { // 생성 시작
            if (owner == null || EventSystem.current == null) // 회차와 입력 관리자 검사
            { // 오류 시작
                throw new InvalidOperationException("Missing run or input system."); // 조작 불가능한 화면 차단
            } // 오류 끝
            var root = new GameObject("TrialNpcScreen", typeof(TrialNpcScreenView)); // 대화 화면 상위 객체
            try // 화면 생성 보호
            { // 생성 보호 시작
                var view = root.GetComponent<TrialNpcScreenView>(); // 대화 화면 제어
                view.owner = owner; // 회차 관리자 연결
                view.Build(); // 상인 화면 구성
                view.SelectDefault(); // 안전한 종료 기본 선택
                return view; // 생성된 화면 전달
            } // 생성 보호 끝
            catch // 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build() // 상인 대화 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("TrialNpcCanvas"); // 상인 전용 화면
            canvas.transform.SetParent(transform, false); // 화면 상위 객체 연결
            canvas.sortingOrder = 115; // 탐험보다 앞과 상점보다 뒤 표시
            var background = new GameObject("TrialNpcBackground", typeof(RectTransform), typeof(Image)); // 전체 화면 배경
            background.transform.SetParent(canvas.transform, false); // 배경의 화면 연결
            var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 상인 화면 바탕색
            UiFactory.Decorate(background.GetComponent<Image>(), "DialoguePanel"); // 화면 용도별 그림 장식
            UiFactory.CreateText(canvas.transform, "TrialNpcTitle", "떠돌이 상인", 48, new Vector2(0f, 420f), new Vector2(1500f, 90f), Color.white); // 상인 화면 제목
            AddPortrait(canvas.transform); // 일본 애니메이션풍 시험 초상화
            dialogue = UiFactory.CreateText(canvas.transform, "TrialNpcDialogue", "어서 와요. 필요한 물건이 있나요?", 32, new Vector2(390f, 115f), new Vector2(970f, 260f), Color.white); // 상인의 현재 대화
            dialogue.resizeTextForBestFit = true; // 긴 대사의 표시 영역 맞춤
            dialogue.resizeTextMinSize = 20; // 대사 최소 글자 크기
            notice = UiFactory.CreateText(canvas.transform, "TrialNpcNotice", "", 24, new Vector2(390f, -145f), new Vector2(1000f, 100f), Color.white); // 상점 저장 오류 안내
            affinity = UiFactory.CreateText(canvas.transform, "TrialNpcAffinity", "", 26, new Vector2(390f, -60f), new Vector2(1000f, 60f), Color.white); // 영구 호감도와 단계
            MakeButton(canvas.transform, "TrialNpcTalk", "대화", new Vector2(-80f, -340f), Talk); // 시험 대화 선택
            friendly = MakeButton(canvas.transform, "TrialNpcFriendlyChoice", "우호 +5 (시험)", new Vector2(200f, -340f), Friendly); // 회차당 한 번의 시험 우호 선택
            MakeButton(canvas.transform, "TrialNpcShop", "상점", new Vector2(480f, -340f), OpenShop); // 시험 상점 선택
            leave = MakeButton(canvas.transform, "TrialNpcLeave", "떠나기", new Vector2(760f, -340f), Close); // 대화 종료 선택
            RefreshAffinity(); // 저장된 관계와 선택 가능 상태 표시
        } // 구성 끝
        private static void AddPortrait(Transform parent) // 상인의 실제 초상화 표시
        { // 표시 시작
            var texture = Resources.Load<Texture2D>("BattlePortraits/MerchantPrototype"); // 상인 초상화 리소스
            if (texture == null) // 이미지 리소스 검사
            { // 누락 시작
                UiFactory.CreateText(parent, "TrialNpcPortraitFallback", "상인", 36, new Vector2(-530f, 30f), new Vector2(500f, 150f), Color.white); // 이미지 누락 시 이름 표시
                return; // 누락된 이미지의 표시 생략
            } // 누락 끝
            var portrait = new GameObject("TrialNpcPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 상인 그림 객체
            portrait.transform.SetParent(parent, false); // 대화 화면에 배치
            var rect = portrait.GetComponent<RectTransform>(); // 그림 표시 영역
            rect.anchoredPosition = new Vector2(-530f, 20f); // 화면 왼쪽의 상인 위치
            var ratio = (float)texture.width / texture.height; // 원본 이미지 가로세로 비율
            var width = Mathf.Min(620f, 720f * ratio); // 화면 영역 안의 그림 너비
            rect.sizeDelta = new Vector2(width, width / ratio); // 원본 비율 유지 크기
            var image = portrait.GetComponent<RawImage>(); // 실제 그림 표시
            image.texture = texture; // 상인 일러스트 연결
            image.raycastTarget = false; // 버튼 입력 가림 방지
        } // 표시 끝
        private Button MakeButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action) // 취소 입력이 연결된 상인 버튼 생성
        { // 생성 시작
            var button = UiFactory.CreateButton(parent, name, label, position); // 공통 화면 버튼
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 84f); // 화면 안의 버튼 크기
            button.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 80f); // 버튼 글자 영역
            button.transform.Find("Label").GetComponent<Text>().resizeTextForBestFit = true; // 긴 버튼 글자의 영역 맞춤
            button.transform.Find("Label").GetComponent<Text>().resizeTextMinSize = 14; // 버튼 글자 최소 크기
            button.onClick.AddListener(action); // 실제 행동 연결
            button.gameObject.AddComponent<TrialNpcCancelRelay>().Initialize(this); // 버튼의 취소 입력 연결
            return button; // 생성된 버튼 전달
        } // 생성 끝
        public void SelectDefault() // 상점 종료 후 기본 선택 복구
        { // 선택 시작
            if (EventSystem.current != null && leave != null && gameObject.activeInHierarchy) // 활성 화면과 입력 관리자 검사
            { // 선택 적용 시작
                RefreshAffinity(); // 현재 관계와 저장 안내 복구
                EventSystem.current.SetSelectedGameObject(leave.gameObject); // 안전한 떠나기 기본 선택
            } // 선택 적용 끝
        } // 선택 끝
        private bool ShopVisible() // 대화 앞의 실제 상점 검사
        { // 검사 시작
            return GameObject.Find("EconomyScreen") != null; // 활성 상점 화면 여부
        } // 검사 끝
        private void Talk() // 시험 상인 대화 진행
        { // 대화 시작
            if (busy || ShopVisible()) // 중복 입력과 상점 표시 검사
            { // 대화 차단 시작
                return; // 상점 뒤의 대화 변경 차단
            } // 대화 차단 끝
            dialogueIndex = (dialogueIndex + 1) % 3; // 시험 대사 순서 순환
            dialogue.text = dialogueIndex == 1 ? "던전에서는 준비가 목숨을 지켜 주죠." : dialogueIndex == 2 ? "물건을 둘러보고 천천히 결정하세요." : "어서 와요. 필요한 물건이 있나요?"; // 현재 시험 대사 표시
            notice.text = owner.RelationshipSaveMessage; // 관계 저장 안내 유지
        } // 대화 끝
        private void RefreshAffinity() // 현재 관계와 시험 선택 가능 상태 표시
        { // 갱신 시작
            affinity.text = owner.TrialMerchantRelationshipText; // 저장된 수치와 관계 단계
            friendly.interactable = owner.CanUseTrialFriendliness; // 현재 회차 중복과 저장 차단 검사
            if (!string.IsNullOrEmpty(owner.RelationshipSaveMessage)) // 복구 또는 저장 오류 검사
            { // 오류 표시 시작
                notice.text = owner.RelationshipSaveMessage; // 실제 관계 저장 안내
            } // 오류 표시 끝
        } // 갱신 끝
        private void Friendly() // 시험 우호 선택과 영구 저장
        { // 선택 시작
            if (busy || ShopVisible()) // 중복 입력과 상점 표시 검사
            { // 차단 시작
                return; // 다른 화면과 중복 입력 차단
            } // 차단 끝
            busy = true; // 저장 중 중복 입력 잠금
            try // 실제 관계 저장 요청
            { // 저장 시작
                owner.TryGrantTrialFriendliness(out var message); // 시험 우호 선택 적용 요청
                notice.text = message; // 실제 결과와 실패 안내
                RefreshAffinity(); // 저장된 수치와 선택 가능 상태 갱신
            } // 저장 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 입력 허용
            } // 복구 끝
        } // 선택 끝
        private void OpenShop() // 상인의 실제 시험 상점 진입
        { // 진입 시작
            if (busy || ShopVisible()) // 중복 입력과 기존 상점 검사
            { // 진입 차단 시작
                return; // 중복 상점 화면 차단
            } // 진입 차단 끝
            busy = true; // 상점 생성 중 중복 입력 잠금
            try // 실제 상점 진입 요청
            { // 요청 시작
                if (!owner.TryOpenNpcShop(out var message)) // 상점 생성과 저장 검사
                { // 오류 시작
                    notice.text = message; // 실패 이유와 재시도 안내
                } // 오류 끝
                else // 정상 상점 진입 분기
                { // 진입 성공 시작
                    notice.text = owner.RelationshipSaveMessage; // 관계 저장 안내 유지
                } // 진입 성공 끝
            } // 요청 끝
            finally // 중복 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 행동 입력 허용
            } // 복구 끝
        } // 진입 끝
        private void Close() // 상인 대화 종료
        { // 종료 시작
            if (busy || ShopVisible()) // 상점 화면과 중복 입력 검사
            { // 종료 차단 시작
                return; // 상점 중 대화 종료 차단
            } // 종료 차단 끝
            owner.CloseTrialNpc(); // 실제 대화 화면 종료 요청
        } // 종료 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 입력
        { // 취소 시작
            eventData.Use(); // 다른 화면의 취소 입력 차단
            Close(); // 상점이 없을 때 대화 종료
        } // 취소 끝
    } // 화면 클래스 끝
    public sealed class TrialNpcCancelRelay : MonoBehaviour, ICancelHandler // 선택 버튼의 취소 입력 중계
    { // 클래스 시작
        private TrialNpcScreenView screen; // 연결된 상인 화면
        public void Initialize(TrialNpcScreenView target) // 상인 화면 연결
        { // 연결 시작
            screen = target; // 대상 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 취소 입력 전달
        { // 전달 시작
            if (screen != null) // 연결된 화면 검사
            { // 전달 조건 시작
                screen.OnCancel(eventData); // 상인 화면에 취소 입력 전달
            } // 전달 조건 끝
        } // 전달 끝
    } // 클래스 끝
} // 이름 공간 끝
