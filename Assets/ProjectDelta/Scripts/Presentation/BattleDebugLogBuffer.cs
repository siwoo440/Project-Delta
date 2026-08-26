using System.Collections.Generic; // 목록 자료형 기능
using System.Text; // 문자열 조립 기능

namespace ProjectDelta.Presentation // 프레젠테이션 영역
{
    public sealed class BattleDebugLogBuffer // 전투 로그 누적 버퍼
    {
        private readonly List<string> lines = new List<string>(); // 누적 로그 목록
        private readonly int maxLineCount; // 최대 로그 줄 수
        private int lastSequence = int.MinValue; // 마지막 행동 시퀀스
        private int lastRound = -1; // 마지막 표시 라운드

        public IReadOnlyList<string> Lines => lines; // 읽기 전용 로그 목록
        public int Count => lines.Count; // 현재 로그 줄 수

        public BattleDebugLogBuffer(int maxLineCount = 200) // 로그 버퍼 생성
        {
            this.maxLineCount = maxLineCount > 0 ? maxLineCount : 200; // 유효한 최대치 저장
        }

        public void BeginBattle() // 새 전투 로그 시작
        {
            BeginBattle(int.MinValue, true); // 기본 초기화 방식 적용
        }

        public void BeginBattle(int currentSequence, bool captureCurrentSequence) // 현재 시퀀스를 고려한 새 전투 시작
        {
            lines.Clear(); // 이전 전투 로그 제거
            lastSequence = captureCurrentSequence ? int.MinValue : currentSequence; // 첫 행동 보존 여부 적용
            lastRound = -1; // 라운드 기록 초기화
            AddLine("=== Battle Start ==="); // 전투 시작선 추가
        }

        public bool TryAppendAction(int sequence, int round, string actorId, string commandId, IReadOnlyList<string> logs) // 새 행동 로그 추가
        {
            if (sequence == lastSequence) // 같은 행동 시퀀스 확인
            {
                return false; // 중복 행동 기록 차단
            }

            lastSequence = sequence; // 새 행동 시퀀스 저장

            if (round > 0 && round != lastRound) // 새 라운드 확인
            {
                lastRound = round; // 현재 라운드 저장
                AddLine($"--- Round {round} ---"); // 라운드 구분선 추가
            }

            string safeActorId = string.IsNullOrEmpty(actorId) ? "UNKNOWN" : actorId; // 행동자 이름 보정
            string safeCommandId = string.IsNullOrEmpty(commandId) ? "Action" : commandId; // 명령 이름 보정
            string roundPrefix = round > 0 ? $"R{round}" : "R?"; // 라운드 접두어 생성

            if (logs == null || logs.Count == 0) // 세부 로그 없음 확인
            {
                AddLine($"[{roundPrefix}] [{safeActorId}] [{safeCommandId}]"); // 기본 행동 로그 추가
                return true; // 기록 성공 반환
            }

            for (int index = 0; index < logs.Count; index++) // 세부 로그 순회
            {
                string log = string.IsNullOrEmpty(logs[index]) ? "(내용 없음)" : logs[index]; // 빈 로그 보정
                AddLine($"[{roundPrefix}] [{safeActorId}] [{safeCommandId}] {log}"); // 행동 상세 로그 추가
            }

            return true; // 기록 성공 반환
        }

        public string BuildDisplayText(int visibleLineCount) // 화면 표시 문자열 생성
        {
            StringBuilder builder = new StringBuilder(); // 문자열 빌더 생성
            builder.AppendLine("Battle Log [F1]"); // 패널 제목 추가

            if (lines.Count == 0) // 기록 없음 확인
            {
                builder.Append("전투 기록 없음"); // 빈 상태 문구 추가
                return builder.ToString(); // 빈 상태 문자열 반환
            }

            int lineCount = visibleLineCount > 0 ? visibleLineCount : lines.Count; // 표시 줄 수 결정
            int startIndex = lines.Count > lineCount ? lines.Count - lineCount : 0; // 시작 줄 위치 계산

            for (int index = startIndex; index < lines.Count; index++) // 표시 대상 로그 순회
            {
                builder.AppendLine(lines[index]); // 로그 한 줄 추가
            }

            return builder.ToString().TrimEnd(); // 마지막 줄바꿈 제거 후 반환
        }

        private void AddLine(string line) // 내부 로그 한 줄 추가
        {
            lines.Add(line); // 새 로그 저장

            while (lines.Count > maxLineCount) // 최대 줄 수 초과 확인
            {
                lines.RemoveAt(0); // 가장 오래된 로그 제거
            }
        }
    }
}
