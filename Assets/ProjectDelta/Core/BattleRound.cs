using System.Collections.Generic; // 참가자와 행동 순서 목록
using System.Linq; // 기존 규칙의 안정 정렬
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class BattleRound // 라운드와 행동 순서 상태
    { // 클래스 시작
        private readonly List<BattleActor> actors; // 전투 참가자 목록
        private List<BattleActor> pending = new List<BattleActor>(); // 현재 라운드 행동 순서
        private int nextIndex; // 다음 행동자 번호
        private BattleActor current; // 행동 중인 참가자
        private bool active; // 라운드 진행 여부
        public int Number // 현재 라운드 번호
        { // 속성 시작
            get; // 라운드 번호 조회
            private set; // 내부 번호 변경
        } // 속성 끝
        public BattleRound(IReadOnlyList<BattleActor> participants) // 전투 참가자 연결
        { // 연결 시작
            actors = participants == null ? new List<BattleActor>() : new List<BattleActor>(participants); // 참가자 목록 복사
        } // 연결 끝
        public bool TryStartRound() // 새 라운드 시작
        { // 시작 처리 시작
            if (active || Number == int.MaxValue) // 중복 시작과 숫자 범위 검사
            { // 시작 거절 조건 시작
                return false; // 잘못된 라운드 시작 차단
            } // 시작 거절 조건 끝
            pending = actors.Where(actor => actor != null && actor.IsAlive).OrderByDescending(actor => actor.Speed).ThenBy(actor => actor.IsPlayer ? 0 : 1).ThenBy(actor => actor.IsPlayer ? 0 : actor.Slot).ToList(); // 생존자 속도와 동률 순서
            if (pending.Count == 0) // 행동 가능한 참가자 검사
            { // 참가자 없음 조건 시작
                return false; // 빈 라운드 시작 차단
            } // 참가자 없음 조건 끝
            nextIndex = 0; // 첫 행동 순서 초기화
            current = null; // 현재 행동자 초기화
            active = true; // 라운드 진행 시작
            Number++; // 새 라운드 번호 확정
            return true; // 라운드 시작 성공
        } // 시작 처리 끝
        public bool TryNext(out BattleActor actor) // 다음 행동자 선택
        { // 선택 시작
            actor = null; // 출력 행동자 초기화
            if (!active || current != null) // 라운드와 진행 중인 행동 검사
            { // 선택 거절 조건 시작
                return false; // 중복 선택 차단
            } // 선택 거절 조건 끝
            SkipDead(); // 행동 전 사망한 참가자 제외
            if (nextIndex >= pending.Count) // 남은 행동자 검사
            { // 행동자 없음 조건 시작
                return false; // 행동 순서 종료
            } // 행동자 없음 조건 끝
            current = pending[nextIndex++]; // 다음 행동자 확정
            actor = current; // 선택한 행동자 전달
            return true; // 선택 성공
        } // 선택 끝
        public bool TryCompleteAction() // 현재 행동 완료
        { // 완료 시작
            if (!active || current == null) // 라운드와 행동자 검사
            { // 완료 거절 조건 시작
                return false; // 없는 행동 완료 차단
            } // 완료 거절 조건 끝
            current = null; // 현재 행동 종료
            return true; // 행동 완료 성공
        } // 완료 끝
        public bool TryEndRound() // 모든 행동 완료 후 라운드 종료
        { // 종료 시작
            if (!active || current != null) // 라운드와 현재 행동 검사
            { // 종료 거절 조건 시작
                return false; // 진행 중인 행동 보존
            } // 종료 거절 조건 끝
            SkipDead(); // 행동 전 사망한 참가자 제외
            if (nextIndex < pending.Count) // 남은 행동자 검사
            { // 행동자 남음 조건 시작
                return false; // 미완료 라운드 종료 차단
            } // 행동자 남음 조건 끝
            active = false; // 완료된 라운드 종료
            return true; // 라운드 종료 성공
        } // 종료 끝
        private void SkipDead() // 행동 대기 중 사망한 참가자 제외
        { // 제외 시작
            while (nextIndex < pending.Count && !pending[nextIndex].IsAlive) // 사망한 대기 참가자 검사
            { // 사망자 제외 시작
                nextIndex++; // 다음 행동자 검사
            } // 사망자 제외 끝
        } // 제외 끝
    } // 클래스 끝
} // 이름 공간 끝
