using UnityEditor; // 편집기 메뉴
namespace ProjectDelta.Editor // 제작 도구 이름 공간
{ // 이름 공간 시작
    public static class DayThirtyNineProjectSetup // 39일차 Windows 빌드
    { // 클래스 시작
        [MenuItem("Project Delta/39일차/Windows 빌드")] // 편집기 빌드 메뉴
        public static void BuildWindows() // 39일차 실행 파일 빌드
        { // 빌드 시작
            DayOneProjectSetup.BuildWindowsAt("Builds/Day39"); // 기존 빌드와 분리된 출력 경로
        } // 빌드 끝
    } // 클래스 끝
} // 이름 공간 끝
