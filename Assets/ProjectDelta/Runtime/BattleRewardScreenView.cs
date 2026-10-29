using ProjectDelta.Core; // 저장된 승리 보상 후보
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 기본 버튼 선택
using UnityEngine.UI; // 보상 안내와 배경
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class BattleRewardScreenView : MonoBehaviour // 저장된 승리 보상 확인 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차와 보상 관리자
        private Text notice; // 저장 오류와 선택 안내
        public static BattleRewardScreenView Create(AppRoot owner, SavedBattleReward reward) // 실제 보상 화면 생성
        { // 생성 시작
            var screen = new GameObject("BattleRewardScreen", typeof(BattleRewardScreenView)); // 보상 화면 상위 객체
            try // 화면 구성
            { // 구성 시작
                var view = screen.GetComponent<BattleRewardScreenView>(); // 실제 화면 제어
                view.owner = owner; // 현재 회차 연결
                var canvas = UiFactory.CreateCanvas("BattleRewardCanvas"); // 보상 전용 화면
                canvas.transform.SetParent(screen.transform, false); // 보상 화면 연결
                canvas.sortingOrder = 110; // 전투보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 화면 배경
                background.transform.SetParent(canvas.transform, false); // 보상 화면에 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치
                rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래 여백 제거
                rect.offsetMax = Vector2.zero; // 위 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 보상 배경색
                UiFactory.CreateText(canvas.transform, "Header", "승리 보상", 58, new Vector2(0f, 300f), new Vector2(1000f, 100f), Color.white); // 결과 제목
                UiFactory.CreateText(canvas.transform, "RewardAmounts", "경험치 " + reward.experience + " · 시험 골드 " + reward.gold, 34, new Vector2(0f, 150f), new Vector2(1100f, 80f), Color.white); // 경험치와 시험 골드
                UiFactory.CreateText(canvas.transform, "RewardItem", reward.potionCount > 0 ? "소형 회복약 " + reward.potionCount + "개" : "획득한 소비 아이템 없음", 30, new Vector2(0f, 55f), new Vector2(900f, 70f), Color.white); // 고정된 소비 아이템 후보
                view.notice = UiFactory.CreateText(canvas.transform, "RewardNotice", "보상을 확인하세요", 24, new Vector2(0f, -70f), new Vector2(1100f, 100f), Color.white); // 획득과 저장 안내
                var take = UiFactory.CreateButton(canvas.transform, "TakeRewardButton", "보상 받기", new Vector2(-215f, -210f)); // 모든 보상 획득
                var skip = UiFactory.CreateButton(canvas.transform, "SkipRewardItemButton", "아이템 포기", new Vector2(215f, -210f)); // 아이템을 두고 보상 확정
                take.onClick.AddListener(() => view.Resolve(true)); // 실제 아이템 획득
                skip.onClick.AddListener(() => view.Resolve(false)); // 명시적 아이템 포기
                skip.gameObject.SetActive(reward.potionCount > 0); // 아이템이 있을 때만 포기 선택
                EventSystem.current.SetSelectedGameObject(take.gameObject); // 기본 보상 획득 선택
                return view; // 준비된 화면 전달
            } // 구성 끝
            catch // 생성 실패 검사
            { // 실패 정리 시작
                screen.SetActive(false); // 불완전한 화면 숨김
                Destroy(screen); // 실패한 화면 제거
                throw; // 실제 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Resolve(bool takePotion) // 명시적인 보상 획득 또는 아이템 포기
        { // 선택 시작
            if (!owner.TryResolveVictoryReward(takePotion, out var error)) // 원자적 보상 확정 검사
            { // 확정 실패 시작
                notice.text = error; // 공간 또는 저장 오류 안내
            } // 확정 실패 끝
        } // 선택 끝
    } // 클래스 끝
} // 이름 공간 끝
