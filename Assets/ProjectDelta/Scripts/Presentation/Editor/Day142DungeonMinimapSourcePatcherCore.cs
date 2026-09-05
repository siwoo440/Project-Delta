using System; // 문자열 비교 기능
using System.Text; // 문자열 조립 기능
using System.Text.RegularExpressions; // OnGUI 탐색 기능

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    internal static class Day142DungeonMinimapSourcePatcherCore // 미니맵 OnGUI 소스 변환기
    {
        internal const string PatchMarker =
            "// DAY142_DUNGEON_MINIMAP_UGUI_PATCH"; // 중복 패치 방지 표식

        internal const string RuntimeMethodName =
            "BuildDungeonMinimapRuntimeUi142"; // 변환 메서드 이름

        internal static bool TryPatch(
            string source,
            out string patchedSource) // 미니맵 소스 변환
        {
            patchedSource =
                source; // 기본 반환값 보존

            if (string.IsNullOrEmpty(source)) // 빈 소스 확인
            {
                return false; // 변환 불가 반환
            }

            if (source.IndexOf(
                    PatchMarker,
                    StringComparison.Ordinal) >= 0) // 패치 표식 확인
            {
                return false; // 중복 패치 차단
            }

            if (source.IndexOf(
                    RuntimeMethodName,
                    StringComparison.Ordinal) >= 0) // 변환 메서드 확인
            {
                return false; // 중복 메서드 차단
            }

            Match methodMatch =
                Regex.Match(
                    source,
                    @"(?m)^(?<indent>[ \t]*)(?<mods>(?:(?:public|private|protected|internal|static|virtual|override|sealed|new|async)\s+)*)void\s+OnGUI\s*\(\s*\)"); // OnGUI 메서드 검색

            if (!methodMatch.Success) // OnGUI 존재 확인
            {
                return false; // 변환 대상 없음
            }

            int openBraceIndex =
                source.IndexOf(
                    '{',
                    methodMatch.Index + methodMatch.Length); // 시작 중괄호 검색

            if (openBraceIndex < 0) // 시작 중괄호 확인
            {
                return false; // 잘못된 구조 반환
            }

            int closeBraceIndex =
                FindMatchingBrace(
                    source,
                    openBraceIndex); // 종료 중괄호 검색

            if (closeBraceIndex < 0) // 종료 중괄호 확인
            {
                return false; // 잘못된 구조 반환
            }

            string indent =
                methodMatch.Groups["indent"].Value; // 기존 들여쓰기 보존

            string methodSource =
                source.Substring(
                    methodMatch.Index,
                    closeBraceIndex - methodMatch.Index + 1); // OnGUI 코드 추출

            string renamedMethod =
                Regex.Replace(
                    methodSource,
                    @"\bOnGUI\b",
                    RuntimeMethodName,
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1)); // 메서드 이름 변경

            StringBuilder builder =
                new StringBuilder(source.Length + 256); // 결과 버퍼 생성

            builder.Append(
                source,
                0,
                methodMatch.Index); // OnGUI 이전 코드 복사

            builder.Append(
                indent); // 표식 들여쓰기 추가

            builder.AppendLine(
                PatchMarker); // 패치 표식 추가

            builder.Append(
                renamedMethod); // 변환 메서드 추가

            builder.Append(
                source,
                closeBraceIndex + 1,
                source.Length - closeBraceIndex - 1); // 나머지 코드 복사

            patchedSource =
                ConvertGuiCalls(
                    builder.ToString()); // 전체 GUI 호출 변환

            return true; // 변환 성공 반환
        }

        private static string ConvertGuiCalls(
            string source) // 미니맵 IMGUI 호출 치환
        {
            string converted =
                source; // 전체 소스 변환 시작

            converted =
                converted.Replace(
                    "UnityEngine.GUIUtility.RotateAroundPivot",
                    "DungeonMinimapRuntimeGuiProxy.RotateAroundPivot"); // 완전 수식 회전 호출 변환

            converted =
                converted.Replace(
                    "GUIUtility.RotateAroundPivot",
                    "DungeonMinimapRuntimeGuiProxy.RotateAroundPivot"); // 회전 호출 변환

            converted =
                converted.Replace(
                    "UnityEngine.GUI.",
                    "DungeonMinimapRuntimeGuiProxy."); // 완전 수식 GUI 호출 변환

            converted =
                converted.Replace(
                    "GUI.",
                    "DungeonMinimapRuntimeGuiProxy."); // 일반 GUI 호출 변환

            return converted; // 변환된 소스 반환
        }

        private static int FindMatchingBrace(
            string source,
            int openBraceIndex) // 문자열과 주석을 무시한 중괄호 검색
        {
            int depth = 0; // 중괄호 깊이
            bool inString = false; // 일반 문자열 상태
            bool inVerbatimString = false; // 축자 문자열 상태
            bool inChar = false; // 문자 상태
            bool inLineComment = false; // 한 줄 주석 상태
            bool inBlockComment = false; // 블록 주석 상태
            bool escaped = false; // 이스케이프 상태

            for (int index = openBraceIndex;
                 index < source.Length;
                 index++) // 소스 문자 순회
            {
                char current =
                    source[index]; // 현재 문자 읽기

                char next =
                    index + 1 < source.Length
                        ? source[index + 1]
                        : '\0'; // 다음 문자 읽기

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
                    if (current == '*'
                        && next == '/') // 블록 주석 종료 확인
                    {
                        inBlockComment = false; // 블록 주석 종료
                        index++; // 종료 문자 건너뛰기
                    }

                    continue; // 주석 문자 건너뛰기
                }

                if (inString) // 일반 문자열 처리
                {
                    if (escaped) // 이스케이프 확인
                    {
                        escaped = false; // 이스케이프 해제
                        continue; // 현재 문자 건너뛰기
                    }

                    if (current == '\\') // 이스케이프 시작 확인
                    {
                        escaped = true; // 다음 문자 이스케이프
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
                    if (current == '"'
                        && next == '"') // 축자 따옴표 이스케이프 확인
                    {
                        index++; // 두 번째 따옴표 건너뛰기
                        continue; // 축자 문자열 계속
                    }

                    if (current == '"') // 축자 문자열 종료 확인
                    {
                        inVerbatimString = false; // 축자 문자열 종료
                    }

                    continue; // 축자 문자열 문자 건너뛰기
                }

                if (inChar) // 문자 리터럴 처리
                {
                    if (escaped) // 문자 이스케이프 확인
                    {
                        escaped = false; // 이스케이프 해제
                        continue; // 현재 문자 건너뛰기
                    }

                    if (current == '\\') // 문자 이스케이프 시작
                    {
                        escaped = true; // 다음 문자 이스케이프
                        continue; // 현재 문자 건너뛰기
                    }

                    if (current == '\'') // 문자 종료 확인
                    {
                        inChar = false; // 문자 리터럴 종료
                    }

                    continue; // 문자 문자 건너뛰기
                }

                if (current == '/'
                    && next == '/') // 한 줄 주석 시작 확인
                {
                    inLineComment = true; // 한 줄 주석 시작
                    index++; // 두 번째 슬래시 건너뛰기
                    continue; // 주석 처리 계속
                }

                if (current == '/'
                    && next == '*') // 블록 주석 시작 확인
                {
                    inBlockComment = true; // 블록 주석 시작
                    index++; // 별표 건너뛰기
                    continue; // 주석 처리 계속
                }

                if (current == '@'
                    && next == '"') // 축자 문자열 시작 확인
                {
                    inVerbatimString = true; // 축자 문자열 시작
                    index++; // 시작 따옴표 건너뛰기
                    continue; // 문자열 처리 계속
                }

                if (current == '"') // 일반 문자열 시작 확인
                {
                    inString = true; // 일반 문자열 시작
                    continue; // 문자열 처리 계속
                }

                if (current == '\'') // 문자 리터럴 시작 확인
                {
                    inChar = true; // 문자 리터럴 시작
                    continue; // 문자 처리 계속
                }

                if (current == '{') // 여는 중괄호 확인
                {
                    depth++; // 깊이 증가
                    continue; // 다음 문자 진행
                }

                if (current == '}') // 닫는 중괄호 확인
                {
                    depth--; // 깊이 감소

                    if (depth == 0) // 최초 블록 종료 확인
                    {
                        return index; // 종료 위치 반환
                    }
                }
            }

            return -1; // 대응 중괄호 없음
        }
    }
}
