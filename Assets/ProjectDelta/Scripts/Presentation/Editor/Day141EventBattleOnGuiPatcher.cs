#if UNITY_EDITOR // Unity Editor 전용 컴파일
using System.IO; // 파일 읽기와 쓰기
using System.Text; // UTF-8 인코딩
using UnityEditor; // Unity Editor API
using UnityEngine; // Unity 로그 기능

[InitializeOnLoad] // 에디터 로드 시 자동 실행
internal static class Day141EventBattleOnGuiPatcher // 이벤트 전투 OnGUI 자동 패처
{
    private const string TargetPath = "Assets/ProjectDelta/Scripts/Presentation/EventBattleController.cs"; // 수정 대상 경로

    static Day141EventBattleOnGuiPatcher() // 자동 패처 초기화
    {
        EditorApplication.delayCall += ApplyPatch; // 에디터 준비 후 패치 예약
    }

    private static void ApplyPatch() // EventBattleController 자동 수정
    {
        if (!File.Exists(TargetPath)) // 대상 파일 존재 확인
        {
            Debug.LogWarning("[Day141] EventBattleController.cs를 찾지 못해 자동 UGUI 패치를 건너뜁니다."); // 대상 없음 안내
            return; // 자동 패치 종료
        }

        string source = File.ReadAllText(TargetPath); // 기존 컨트롤러 소스 읽기

        if (!Day141EventBattleSourcePatcherCore.TryPatch(source, out string patchedSource)) // OnGUI 변환 시도
        {
            return; // 이미 적용됐거나 대상 없음 종료
        }

        File.WriteAllText(TargetPath, patchedSource, new UTF8Encoding(false)); // 변환 결과 원본에 덮어쓰기
        AssetDatabase.ImportAsset(TargetPath, ImportAssetOptions.ForceUpdate); // 수정된 스크립트 재임포트
        Debug.Log("[Day141] EventBattleController OnGUI를 런타임 UGUI 빌더로 자동 변환했습니다."); // 자동 패치 완료 안내
    }
}
#endif
