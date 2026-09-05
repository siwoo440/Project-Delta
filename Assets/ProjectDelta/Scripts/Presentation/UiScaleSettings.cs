using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 136일차: 기획서 8.1절 "UI 배율 옵션(소/보통/대)" - OnGUI 임시 화면과 Canvas 갤러리
    // 화면 양쪽이 같은 배율 값을 쓰도록 모아둔다. 값 자체는 SettingsData.Ui.UiScale에
    // 저장되고, 화면이 열릴 때 Refresh()로 다시 읽어온다(설정 화면에서 바뀌면 그 화면이
    // 직접 Refresh()를 불러 즉시 반영한다).
    public static class UiScaleSettings
    {
        public const float Small = 0.85f;
        public const float Normal = 1f;
        public const float Large = 1.2f;

        public static float CurrentMultiplier { get; private set; } = Normal;

        public static void Refresh()
        {
            SettingsData settings =
                ApplicationFlow.Current?.ReadOrCreateSettings();

            float saved =
                settings != null
                    ? settings.Ui.UiScale
                    : Normal;

            CurrentMultiplier =
                saved > 0f
                    ? saved
                    : Normal;
        }

        // OnGUI 화면 전용 - 화면 중앙을 기준으로 기존 Rect 좌표 계산을 그대로 두고
        // 그리기 결과만 확대/축소한다. 반드시 OnGUI 끝에서 RestoreGuiMatrix()로 되돌린다.
        public static Matrix4x4 ApplyGuiMatrix()
        {
            Matrix4x4 previousMatrix =
                GUI.matrix;

            Vector2 pivot =
                new Vector2(
                    Screen.width / 2f,
                    Screen.height / 2f);

            GUIUtility.ScaleAroundPivot(
                new Vector2(
                    CurrentMultiplier,
                    CurrentMultiplier),
                pivot);

            return previousMatrix;
        }

        public static void RestoreGuiMatrix(
            Matrix4x4 previousMatrix)
        {
            GUI.matrix =
                previousMatrix;
        }

        // Canvas 화면(CG·도전과제 갤러리 등) 전용 - CanvasScaler(ScaleWithScreenSize)가
        // referenceResolution 대비 화면 크기로 배율을 스스로 계산하는 원리를 그대로
        // 이용한다. referenceResolution을 배율만큼 줄이면 같은 화면 크기에서 더 큰
        // scaleFactor가 나와 UI가 커진다 - CanvasScaler의 매 프레임 재계산과 충돌하지
        // 않는다(값 자체를 대신 넘겨주는 방식이라).
        public static void ApplyToCanvasScaler(
            CanvasScaler scaler,
            Vector2 baseReferenceResolution)
        {
            if (scaler == null)
            {
                return;
            }

            scaler.referenceResolution =
                baseReferenceResolution
                / Mathf.Max(0.01f, CurrentMultiplier);
        }
    }
}
