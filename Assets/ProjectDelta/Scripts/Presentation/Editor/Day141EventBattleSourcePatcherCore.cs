using System; // 문자열 비교 기능
using System.Text; // 문자열 조립 기능
using System.Text.RegularExpressions; // OnGUI 탐색 기능

internal static class Day141EventBattleSourcePatcherCore // 이벤트 전투 소스 자동 변환기
{
    internal const string PatchMarker = "// DAY141_EVENT_BATTLE_UGUI_PATCH"; // 중복 패치 방지 표식
    internal const string RuntimeMethodName = "BuildEventBattleRuntimeUi141"; // 런타임 UI 빌더 이름

    internal static bool TryPatch(string source, out string patchedSource) // EventBattleController 소스 변환
    {
        patchedSource = source; // 기본 반환값 보존

        if (string.IsNullOrEmpty(source)) // 빈 소스 확인
        {
            return false; // 변환 불가 반환
        }

        if (source.IndexOf(PatchMarker, StringComparison.Ordinal) >= 0) // 기존 패치 표식 확인
        {
            return false; // 중복 변환 차단
        }

        if (source.IndexOf(RuntimeMethodName, StringComparison.Ordinal) >= 0) // 동일 런타임 빌더 이름 확인
        {
            return false; // 중복 메서드 생성 차단
        }

        Match methodMatch = Regex.Match(source, @"(?m)^(?<indent>[ \t]*)(?<mods>(?:(?:public|private|protected|internal|static|virtual|override|sealed|new|async)\s+)*)void\s+OnGUI\s*\(\s*\)"); // OnGUI 메서드 검색

        if (!methodMatch.Success) // OnGUI 검색 결과 확인
        {
            return false; // 대상 메서드 없음 반환
        }

        int openBraceIndex = source.IndexOf('{', methodMatch.Index + methodMatch.Length); // 메서드 시작 중괄호 검색

        if (openBraceIndex < 0) // 시작 중괄호 확인
        {
            return false; // 잘못된 메서드 구조 반환
        }

        int closeBraceIndex = FindMatchingBrace(source, openBraceIndex); // 메서드 종료 중괄호 검색

        if (closeBraceIndex < 0) // 종료 중괄호 확인
        {
            return false; // 잘못된 메서드 구조 반환
        }

        string indent = methodMatch.Groups["indent"].Value; // 기존 들여쓰기 보존
        string methodSource = source.Substring(methodMatch.Index, closeBraceIndex - methodMatch.Index + 1); // OnGUI 전체 코드 추출
        string renamedMethod = RenameMethod(methodSource); // OnGUI 메서드 이름만 런타임 빌더로 변경
        StringBuilder builder = new StringBuilder(source.Length + 256); // 결과 문자열 버퍼 생성
        builder.Append(source, 0, methodMatch.Index); // OnGUI 이전 코드 복사
        builder.Append(indent); // 표식 들여쓰기 적용
        builder.Append(PatchMarker); // 패치 표식 추가
        builder.AppendLine(); // 표식 줄 종료
        builder.Append(renamedMethod); // 이름 변경된 메서드 추가
        builder.Append(source, closeBraceIndex + 1, source.Length - closeBraceIndex - 1); // 나머지 코드 복사
        patchedSource = ConvertGuiCalls(builder.ToString()); // 컨트롤러 전체 GUI 호출을 프록시로 변환
        return true; // 변환 성공 반환
    }

    private static string RenameMethod(string methodSource) // OnGUI 메서드 이름 변경
    {
        Regex methodNameRegex = new Regex(@"\bOnGUI\b"); // OnGUI 이름 검색식 생성
        return methodNameRegex.Replace(methodSource, RuntimeMethodName, 1); // 런타임 UI 빌더 이름 반환
    }

    private static string ConvertGuiCalls(string source) // 컨트롤러 전체 IMGUI 호출 변환
    {
        string converted = source; // 전체 소스 변환 시작
        converted = converted.Replace("UnityEngine.GUIUtility.ExitGUI()", "EventBattleRuntimeGuiProxy.ExitGUI()"); // 완전 수식 ExitGUI 호출 대체
        converted = converted.Replace("UnityEngine.GUILayoutUtility.", "EventBattleRuntimeGuiProxy."); // 완전 수식 GUILayoutUtility 호출 대체
        converted = converted.Replace("UnityEngine.GUILayout.", "EventBattleRuntimeGuiProxy."); // 완전 수식 GUILayout 호출 대체
        converted = converted.Replace("UnityEngine.GUI.", "EventBattleRuntimeGuiProxy."); // 완전 수식 GUI 호출 대체
        converted = converted.Replace("GUIUtility.ExitGUI()", "EventBattleRuntimeGuiProxy.ExitGUI()"); // ExitGUI 호출 대체
        converted = converted.Replace("GUILayoutUtility.", "EventBattleRuntimeGuiProxy."); // GUILayoutUtility 호출 대체
        converted = converted.Replace("GUILayout.", "EventBattleRuntimeGuiProxy."); // GUILayout 호출 대체
        converted = converted.Replace("GUI.", "EventBattleRuntimeGuiProxy."); // GUI 호출 대체
        converted = converted.Replace("Event.current", "EventBattleRuntimeGuiProxy.CurrentEvent"); // 현재 IMGUI 이벤트 대체
        return converted; // 전체 변환 소스 반환
    }

    private static int FindMatchingBrace(string source, int openBraceIndex) // 문자열과 주석을 무시한 중괄호 대응 검색
    {
        int depth = 0; // 현재 중괄호 깊이
        bool inString = false; // 일반 문자열 상태
        bool inVerbatimString = false; // 축자 문자열 상태
        bool inChar = false; // 문자 리터럴 상태
        bool inLineComment = false; // 한 줄 주석 상태
        bool inBlockComment = false; // 블록 주석 상태
        bool escaped = false; // 이스케이프 상태

        for (int index = openBraceIndex; index < source.Length; index++) // 소스 문자 순회
        {
            char current = source[index]; // 현재 문자 읽기
            char next = index + 1 < source.Length ? source[index + 1] : '\0'; // 다음 문자 읽기

            if (inLineComment) // 한 줄 주석 처리
            {
                if (current == '\n') // 줄 종료 확인
                {
                    inLineComment = false; // 한 줄 주석 종료
                }

                continue; // 주석 문자 건너뛰기
            }

            if (inBlockComment) // 블록 주석 처리
            {
                if (current == '*' && next == '/') // 블록 주석 종료 확인
                {
                    inBlockComment = false; // 블록 주석 종료
                    index++; // 종료 슬래시 건너뛰기
                }

                continue; // 주석 문자 건너뛰기
            }

            if (inString) // 일반 문자열 처리
            {
                if (escaped) // 이스케이프 문자 확인
                {
                    escaped = false; // 이스케이프 상태 해제
                    continue; // 현재 문자 건너뛰기
                }

                if (current == '\\') // 이스케이프 시작 확인
                {
                    escaped = true; // 다음 문자 이스케이프 설정
                    continue; // 현재 문자 건너뛰기
                }

                if (current == '"') // 문자열 종료 확인
                {
                    inString = false; // 일반 문자열 종료
                }

                continue; // 문자열 문자 건너뛰기
            }

            if (inVerbatimString) // 축자 문자열 처리
            {
                if (current == '"' && next == '"') // 축자 문자열 따옴표 이스케이프 확인
                {
                    index++; // 두 번째 따옴표 건너뛰기
                    continue; // 문자열 처리 계속
                }

                if (current == '"') // 축자 문자열 종료 확인
                {
                    inVerbatimString = false; // 축자 문자열 종료
                }

                continue; // 문자열 문자 건너뛰기
            }

            if (inChar) // 문자 리터럴 처리
            {
                if (escaped) // 문자 이스케이프 확인
                {
                    escaped = false; // 이스케이프 상태 해제
                    continue; // 현재 문자 건너뛰기
                }

                if (current == '\\') // 문자 이스케이프 시작 확인
                {
                    escaped = true; // 다음 문자 이스케이프 설정
                    continue; // 현재 문자 건너뛰기
                }

                if (current == '\'') // 문자 리터럴 종료 확인
                {
                    inChar = false; // 문자 리터럴 종료
                }

                continue; // 문자 리터럴 문자 건너뛰기
            }

            if (current == '/' && next == '/') // 한 줄 주석 시작 확인
            {
                inLineComment = true; // 한 줄 주석 상태 설정
                index++; // 두 번째 슬래시 건너뛰기
                continue; // 주석 처리 계속
            }

            if (current == '/' && next == '*') // 블록 주석 시작 확인
            {
                inBlockComment = true; // 블록 주석 상태 설정
                index++; // 별표 건너뛰기
                continue; // 주석 처리 계속
            }

            if (current == '@' && next == '"') // 축자 문자열 시작 확인
            {
                inVerbatimString = true; // 축자 문자열 상태 설정
                index++; // 시작 따옴표 건너뛰기
                continue; // 문자열 처리 계속
            }

            if (current == '"') // 일반 문자열 시작 확인
            {
                inString = true; // 일반 문자열 상태 설정
                continue; // 문자열 처리 계속
            }

            if (current == '\'') // 문자 리터럴 시작 확인
            {
                inChar = true; // 문자 리터럴 상태 설정
                continue; // 문자 처리 계속
            }

            if (current == '{') // 여는 중괄호 확인
            {
                depth++; // 중괄호 깊이 증가
                continue; // 다음 문자 진행
            }

            if (current == '}') // 닫는 중괄호 확인
            {
                depth--; // 중괄호 깊이 감소

                if (depth == 0) // 최초 메서드 중괄호 종료 확인
                {
                    return index; // 종료 위치 반환
                }
            }
        }

        return -1; // 대응 중괄호 없음 반환
    }
}
