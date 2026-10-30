using System; // 설정 요청과 닫기 동작
using ProjectDelta.Core; // 검증된 설정 형식
using UnityEngine; // 화면 객체 구성
using UnityEngine.EventSystems; // 취소 입력과 선택
using UnityEngine.UI; // 버튼과 글자
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class SettingsScreenView : MonoBehaviour, ICancelHandler // 설정 모달 화면
    { // 화면 구성 시작
        private Func<GameSettings> current; // 저장된 설정 조회
        private Func<GameSettings, bool> apply; // 설정 저장과 적용
        private Action close; // 이전 화면 복귀
        private Text volumeValue; // 전체 음량 표시
        private Text flashValue; // 조우 섬광 표시
        private Text notice; // 저장 결과 안내
        private bool busy; // 중복 설정 입력 방지
        private bool closing; // 중복 닫기 방지
        public static GameObject Create(Transform parent, Func<GameSettings> current, Func<GameSettings, bool> apply, Action close) // 설정 화면 생성
        { // 생성 시작
            if (parent == null || current == null || apply == null || close == null || EventSystem.current == null) // 화면과 입력 준비 검사
            { // 준비 실패 시작
                throw new InvalidOperationException("Settings screen requires a parent, settings and active input."); // 잘못된 화면 생성 차단
            } // 준비 실패 끝
            var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image), typeof(SettingsScreenView)); // 모달 배경과 화면 생성
            try // 화면 구성 시도
            { // 구성 시작
                panel.transform.SetParent(parent, false); // 이전 화면 앞 배치
                var rect = panel.GetComponent<RectTransform>(); // 화면 영역 조회
                rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래 여백 제거
                rect.offsetMax = Vector2.zero; // 위 여백 제거
                panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 1f); // 불투명 모달 배경
                UiFactory.Decorate(panel.GetComponent<Image>(), "MenuPanel"); // 화면 용도별 그림 장식
                var view = panel.GetComponent<SettingsScreenView>(); // 모달 제어 조회
                view.current = current; // 현재 설정 연결
                view.apply = apply; // 저장 동작 연결
                view.close = close; // 닫기 동작 연결
                view.Build(); // 설정 화면 구성
                return panel; // 완성 화면 전달
            } // 구성 끝
            catch // 화면 생성 실패 처리
            { // 실패 정리 시작
                panel.SetActive(false); // 불완전한 화면 숨김
                Destroy(panel); // 임시 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build() // 설정 표시와 버튼 구성
        { // 구성 시작
            UiFactory.CreateText(transform, "SettingsHeader", "설정", 52, new Vector2(0f, 410f), new Vector2(1200f, 90f), Color.white); // 화면 제목
            UiFactory.CreateText(transform, "SettingsVolumeLabel", "전체 음량", 32, new Vector2(-400f, 260f), new Vector2(360f, 70f), Color.white); // 전체 음량 제목
            volumeValue = UiFactory.CreateText(transform, "SettingsVolumeValue", "", 32, new Vector2(0f, 260f), new Vector2(220f, 70f), Color.white); // 실제 음량 표시
            MakeButton("SettingsVolumeDown", "−", new Vector2(240f, 260f), () => ChangeVolume(-10)); // 음량 감소 버튼
            MakeButton("SettingsVolumeUp", "+", new Vector2(470f, 260f), () => ChangeVolume(10)); // 음량 증가 버튼
            UiFactory.CreateText(transform, "SettingsFlashLabel", "조우 섬광", 32, new Vector2(-400f, 100f), new Vector2(360f, 70f), Color.white); // 조우 섬광 제목
            flashValue = UiFactory.CreateText(transform, "SettingsFlashValue", "", 30, new Vector2(0f, 100f), new Vector2(250f, 70f), Color.white); // 실제 섬광 설정
            MakeButton("SettingsFlashNormal", "기본", new Vector2(-270f, -20f), () => ChangeFlash(0)); // 기본 섬광 선택
            MakeButton("SettingsFlashReduced", "감소", new Vector2(0f, -20f), () => ChangeFlash(1)); // 감소 섬광 선택
            MakeButton("SettingsFlashOff", "끔", new Vector2(270f, -20f), () => ChangeFlash(2)); // 섬광 끄기 선택
            UiFactory.CreateText(transform, "SettingsUnsupported", "BGM·효과음 개별 설정, 조작 변경 및 기타 접근성 설정은 준비 중", 23, new Vector2(0f, -170f), new Vector2(1650f, 75f), new Color(0.65f, 0.73f, 0.74f)); // 아직 지원하지 않는 설정
            notice = UiFactory.CreateText(transform, "SettingsNotice", "", 23, new Vector2(0f, -260f), new Vector2(1500f, 75f), new Color(1f, 0.72f, 0.55f)); // 실제 저장 결과 안내
            var button = UiFactory.CreateButton(transform, "CloseSettingsButton", "닫기", new Vector2(0f, -400f)); // 설정 닫기 버튼
            button.onClick.AddListener(Close); // 이전 화면 복귀 연결
            button.gameObject.AddComponent<SettingsCancelRelay>().Owner = this; // 닫기 버튼 취소 입력
            Refresh(); // 저장된 실제 설정 표시
            if (AppRoot.Instance != null && !string.IsNullOrEmpty(AppRoot.Instance.SettingsSaveMessage)) // 초기 설정 보호 안내 검사
            { // 복구 안내 시작
                notice.text = AppRoot.Instance.SettingsSaveMessage + " 표시 값은 기본 또는 복구 설정입니다."; // 원본 보호와 대체 설정 안내
            } // 복구 안내 끝
            EventSystem.current.SetSelectedGameObject(button.gameObject); // 기본 닫기 선택
        } // 구성 끝
        private void MakeButton(string name, string label, Vector2 position, Action action) // 설정 버튼 구성
        { // 버튼 생성 시작
            var button = UiFactory.CreateButton(transform, name, label, position); // 공통 버튼 생성
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 70f); // 설정 버튼 크기
            var text = button.GetComponentInChildren<Text>(); // 버튼 글자 조회
            text.rectTransform.sizeDelta = new Vector2(190f, 70f); // 글자 영역 크기
            button.onClick.AddListener(() => action()); // 설정 요청 연결
            button.gameObject.AddComponent<SettingsCancelRelay>().Owner = this; // 선택 버튼 취소 입력
        } // 버튼 생성 끝
        private void ChangeVolume(int delta) // 전체 음량 변경 요청
        { // 요청 시작
            if (busy || closing) // 중복 입력 검사
            { // 중복 입력 시작
                return; // 중복 변경 차단
            } // 중복 입력 끝
            var value = current(); // 저장된 설정 조회
            var volume = Mathf.Clamp(value.MasterVolume + delta, 0, 100); // 허용 범위 음량
            Save(new GameSettings(volume, value.EncounterFlash)); // 독립 후보 저장 요청
        } // 요청 끝
        private void ChangeFlash(int mode) // 조우 섬광 변경 요청
        { // 요청 시작
            if (busy || closing) // 중복 입력 검사
            { // 중복 입력 시작
                return; // 중복 변경 차단
            } // 중복 입력 끝
            var value = current(); // 저장된 설정 조회
            Save(new GameSettings(value.MasterVolume, mode)); // 독립 후보 저장 요청
        } // 요청 끝
        private void Save(GameSettings candidate) // 설정 저장 요청과 실제 결과 표시
        { // 저장 시작
            busy = true; // 중복 요청 잠금
            try // 저장과 반영 시도
            { // 저장 시도 시작
                if (apply(candidate)) // 설정 저장 성공 검사
                { // 저장 성공 시작
                    Refresh(); // 실제 적용 설정만 표시
                    notice.text = "설정을 저장했습니다."; // 실제 성공 안내
                } // 저장 성공 끝
                else // 설정 저장 실패
                { // 저장 실패 시작
                    notice.text = "설정을 저장하지 못했습니다. 기존 설정을 유지합니다."; // 실제 실패 안내
                } // 저장 실패 끝
            } // 저장 시도 끝
            catch (Exception) // 설정 저장 오류
            { // 오류 처리 시작
                notice.text = "설정을 저장하지 못했습니다. 기존 설정을 확인해 주세요."; // 오류 안내
            } // 오류 처리 끝
            finally // 요청 잠금 해제
            { // 해제 시작
                busy = false; // 다음 설정 입력 허용
            } // 해제 끝
        } // 저장 끝
        private void Refresh() // 저장된 설정 표시
        { // 표시 시작
            var value = current(); // 실제 설정 조회
            volumeValue.text = value.MasterVolume + "%"; // 저장된 전체 음량
            flashValue.text = value.EncounterFlash == 0 ? "기본" : value.EncounterFlash == 1 ? "감소" : "끔"; // 저장된 섬광 상태
        } // 표시 끝
        public void OnCancel(BaseEventData eventData) // 키보드와 게임패드 취소
        { // 취소 시작
            eventData.Use(); // 중복 취소 전파 방지
            Close(); // 이전 화면 복귀
        } // 취소 끝
        private void Close() // 설정 화면 닫기
        { // 닫기 시작
            if (closing || busy) // 중복 입력 검사
            { // 중복 입력 시작
                return; // 중복 닫기 차단
            } // 중복 입력 끝
            closing = true; // 닫기 진행 표시
            close(); // 이전 화면 복귀 요청
        } // 닫기 끝
    } // 화면 구성 끝
    public sealed class SettingsCancelRelay : MonoBehaviour, ICancelHandler // 선택 버튼 취소 전달
    { // 전달 구성 시작
        public SettingsScreenView Owner { get; set; } // 현재 설정 화면
        public void OnCancel(BaseEventData eventData) // 선택 버튼 취소 처리
        { // 취소 처리 시작
            if (Owner != null) // 설정 화면 존재 검사
            { // 전달 조건 시작
                Owner.OnCancel(eventData); // 설정 화면 취소 전달
            } // 전달 조건 끝
        } // 취소 처리 끝
    } // 전달 구성 끝
} // 이름 공간 끝
