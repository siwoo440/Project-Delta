using ProjectDelta.Application; // ApplicationFlow.Current 사용
using ProjectDelta.Data; // SettingsData 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: 항목 없는 임시 화면이었다가, 136일차에 기획서 8.1절 "UI 배율 옵션"과
    // "자막 지원"을 실제로 켜고 저장하는 화면으로 채웠다.
    // TODO: 그래픽/사운드/키 바인딩 등 나머지 설정 항목은 해당 시스템이 생기는 일차에서 채운다.
    public sealed class SettingsSceneController : MonoBehaviour // 설정 화면 버튼 제어
    {
        private GUIStyle labelStyle; // 안내 글자 스타일
        private GUIStyle buttonStyle; // 버튼 글자 스타일
        private GUIStyle selectedButtonStyle; // 선택된 배율 버튼 스타일

        private SettingsData settings; // 현재 설정 값

        private void OnEnable() // 설정 화면 진입 시 최신 값 불러오기
        {
            settings = // 저장된 설정 읽기(없으면 기본값)
                ApplicationFlow.Current?.ReadOrCreateSettings()
                ?? new SettingsData();

            UiScaleSettings.Refresh(); // 다른 화면과 같은 배율 값을 공유하도록 갱신
        }

        private void OnGUI() // 설정 UI 표시
        {
            EnsureStyles(); // 스타일 준비

            Matrix4x4 previousMatrix = // 136일차: UI 배율 적용 - 끝에서 반드시 복원
                UiScaleSettings.ApplyGuiMatrix();

            float centerX = Screen.width / 2f; // 화면 가로 중앙 좌표
            GUI.Label(new Rect(centerX - 200f, Screen.height * 0.18f, 400f, 60f), "설정", labelStyle); // 제목 표시

            float buttonWidth = 220f; // 버튼 가로 크기
            float buttonHeight = 50f; // 버튼 세로 크기
            float spacing = buttonHeight + 16f; // 버튼 사이 간격 포함 세로 이동량
            float y = Screen.height * 0.32f; // 첫 항목 세로 시작 좌표

            GUI.Label( // UI 배율 항목 라벨
                new Rect(centerX - 200f, y, 400f, 28f),
                "UI 배율",
                labelStyle);

            y += 36f; // 라벨 다음 줄로 이동

            DrawUiScaleButtons( // 소/보통/대 3단 버튼
                centerX,
                y,
                buttonWidth,
                buttonHeight);

            y += spacing + 20f; // 다음 항목으로 이동(구분 여백 추가)

            bool subtitlesOn = // 현재 자막 표시 여부
                settings.Accessibility.SfxSubtitles;

            if (GUI.Button( // 자막 표시 토글 버튼
                    new Rect(centerX - (buttonWidth / 2f), y, buttonWidth, buttonHeight),
                    subtitlesOn
                        ? "자막 표시: 켜짐"
                        : "자막 표시: 꺼짐",
                    buttonStyle))
            {
                settings.Accessibility.SfxSubtitles = // 자막 표시 반전
                    !subtitlesOn;

                SaveSettings(); // 즉시 저장(기획서 9.1 - 설정은 변경 즉시 저장)
            }

            y += spacing + 20f; // 다음 항목으로 이동

            if (GUI.Button(new Rect(centerX - (buttonWidth / 2f), y, buttonWidth, buttonHeight), "뒤로가기", buttonStyle)) // 뒤로가기 버튼
            {
                ApplicationFlow.Current?.ReturnToTitle(); // 타이틀 화면으로 이동
            }

            UiScaleSettings.RestoreGuiMatrix( // 136일차: 배율 적용 복원
                previousMatrix);
        }

        private void DrawUiScaleButtons(
            float centerX,
            float y,
            float totalButtonWidth,
            float buttonHeight)
        {
            float optionWidth = // 3단 버튼 하나당 가로 크기
                (totalButtonWidth - 16f) / 3f;

            DrawUiScaleOption( // 소 배율 버튼
                centerX - (totalButtonWidth / 2f),
                y,
                optionWidth,
                buttonHeight,
                "소",
                UiScaleSettings.Small);

            DrawUiScaleOption( // 보통 배율 버튼
                centerX - (totalButtonWidth / 2f) + optionWidth + 8f,
                y,
                optionWidth,
                buttonHeight,
                "보통",
                UiScaleSettings.Normal);

            DrawUiScaleOption( // 대 배율 버튼
                centerX - (totalButtonWidth / 2f) + ((optionWidth + 8f) * 2f),
                y,
                optionWidth,
                buttonHeight,
                "대",
                UiScaleSettings.Large);
        }

        private void DrawUiScaleOption(
            float x,
            float y,
            float width,
            float height,
            string label,
            float scaleValue)
        {
            bool isCurrent = // 현재 선택된 배율인지 확인(오차 허용 비교)
                Mathf.Abs(
                    settings.Ui.UiScale
                    - scaleValue)
                < 0.01f;

            if (GUI.Button(
                    new Rect(x, y, width, height),
                    label,
                    isCurrent
                        ? selectedButtonStyle
                        : buttonStyle))
            {
                settings.Ui.UiScale = // 선택한 배율 저장
                    scaleValue;

                SaveSettings(); // 즉시 저장 + 다른 화면과 공유하는 값 갱신
            }
        }

        private void SaveSettings()
        {
            ApplicationFlow.Current?.SaveSettings( // 설정 파일에 즉시 저장
                settings);

            UiScaleSettings.Refresh(); // 이 화면에서 바뀐 배율을 곧바로 반영
        }

        private void EnsureStyles() // GUI 스타일 최초 1회 생성
        {
            if (labelStyle == null) // 안내 스타일 존재 확인
            {
                labelStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복제
                labelStyle.alignment = TextAnchor.MiddleCenter; // 가운데 정렬 적용
                labelStyle.fontSize = 24; // 안내 글자 크기 적용
                labelStyle.normal.textColor = Color.white; // 흰색 적용
            }

            if (buttonStyle == null) // 버튼 스타일 존재 확인
            {
                buttonStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                buttonStyle.fontSize = 18; // 버튼 글자 크기 적용
            }

            if (selectedButtonStyle == null) // 선택된 배율 버튼 스타일 존재 확인
            {
                selectedButtonStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                selectedButtonStyle.fontSize = 18; // 버튼 글자 크기 적용
                selectedButtonStyle.fontStyle = FontStyle.Bold; // 굵게 적용
                selectedButtonStyle.normal.textColor = new Color(0.55f, 0.85f, 1f, 1f); // 선택 강조 색상 적용
            }
        }
    }
}
