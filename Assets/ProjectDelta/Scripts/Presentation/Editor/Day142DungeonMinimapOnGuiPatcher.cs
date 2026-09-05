#if UNITY_EDITOR // Unity Editor 전용 컴파일
using System.IO; // 파일 입출력 기능
using System.Text; // UTF-8 인코딩
using UnityEditor; // Unity Editor API
using UnityEngine; // Unity 로그 기능

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    [InitializeOnLoad] // 에디터 로드 시 자동 실행
    internal static class Day142DungeonMinimapOnGuiPatcher // 미니맵 OnGUI 자동 패처
    {
        private const string TargetPath =
            "Assets/ProjectDelta/Scripts/Presentation/DungeonMinimapController.cs"; // 수정 대상 경로

        static Day142DungeonMinimapOnGuiPatcher() // 자동 패처 초기화
        {
            EditorApplication.delayCall +=
                ApplyPatch; // 에디터 준비 후 패치 예약
        }

        private static void ApplyPatch() // 미니맵 컨트롤러 자동 수정
        {
            if (!File.Exists(TargetPath)) // 대상 파일 확인
            {
                Debug.LogWarning(
                    "[Day142] DungeonMinimapController.cs를 찾지 못해 UGUI 패치를 건너뜁니다."); // 대상 없음 안내
                return; // 자동 패치 종료
            }

            string source =
                File.ReadAllText(
                    TargetPath); // 기존 컨트롤러 읽기

            if (!Day142DungeonMinimapSourcePatcherCore.TryPatch(
                    source,
                    out string patchedSource)) // OnGUI 변환 시도
            {
                return; // 이미 적용됐거나 대상 없음
            }

            File.WriteAllText(
                TargetPath,
                patchedSource,
                new UTF8Encoding(false)); // 변환 결과 원본 덮어쓰기

            AssetDatabase.ImportAsset(
                TargetPath,
                ImportAssetOptions.ForceUpdate); // 수정 스크립트 재임포트

            Debug.Log(
                "[Day142] DungeonMinimapController OnGUI를 런타임 UGUI 빌더로 자동 변환했습니다."); // 패치 완료 안내
        }
    }
}
#endif
