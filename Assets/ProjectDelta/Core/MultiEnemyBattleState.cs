using System; // 잘못된 전투 구성 예외
using System.Collections.Generic; // 전투 참가자 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class MultiEnemyBattleState // 다수 적 대상 선택과 승패 상태
    { // 클래스 시작
        public const int MaxEnemies = 3; // 한 전투의 최대 적 수
        private readonly BattleActor player; // 전투에 참가한 플레이어
        private readonly IReadOnlyList<BattleActor> enemies; // 고정된 적 참가자 목록
        private BattleActor selected; // 플레이어가 직접 선택한 대상
        public IReadOnlyList<BattleActor> Enemies => enemies; // 읽기 전용 적 참가자
        public BattleActor SelectedEnemy // 살아 있는 현재 선택 대상
        { // 속성 시작
            get // 선택 대상 조회
            { // 조회 시작
                if (!player.IsAlive || (selected != null && !selected.IsAlive)) // 플레이어와 선택 대상 생존 검사
                { // 선택 해제 시작
                    selected = null; // 사망한 대상의 자동 선택 해제
                } // 선택 해제 끝
                return selected; // 명시적으로 선택한 생존 대상 전달
            } // 조회 끝
        } // 속성 끝
        public CombatOutcome Outcome // 플레이어와 모든 적의 현재 승패
        { // 속성 시작
            get // 승패 조회
            { // 조회 시작
                if (!player.IsAlive) // 플레이어 사망 검사
                { // 패배 조건 시작
                    return CombatOutcome.Defeat; // 동시 사망의 플레이어 패배 우선
                } // 패배 조건 끝
                foreach (var enemy in enemies) // 모든 적 생존 상태 순회
                { // 생존 적 검사 시작
                    if (enemy.IsAlive) // 생존한 적 검사
                    { // 전투 지속 조건 시작
                        return CombatOutcome.Ongoing; // 한 명이라도 생존하면 전투 지속
                    } // 전투 지속 조건 끝
                } // 생존 적 검사 끝
                return CombatOutcome.Victory; // 모든 적 처치의 승리
            } // 조회 끝
        } // 속성 끝
        public MultiEnemyBattleState(BattleActor player, IReadOnlyList<BattleActor> enemies) // 플레이어와 적 참가자 구성
        { // 구성 시작
            if (player == null || !player.IsPlayer || enemies == null || enemies.Count < 1 || enemies.Count > MaxEnemies) // 플레이어와 적 수 검사
            { // 구성 오류 시작
                throw new ArgumentException("Invalid battle participants."); // 잘못된 전투 시작 차단
            } // 구성 오류 끝
            var unique = new HashSet<BattleActor>(); // 중복 개체 검사 목록
            var copy = new List<BattleActor>(); // 변경되지 않는 참가자 목록
            foreach (var enemy in enemies) // 적 참가자 순회
            { // 참가자 검사 시작
                if (enemy == null || enemy.IsPlayer || !unique.Add(enemy)) // 누락과 아군 및 중복 검사
                { // 적 오류 시작
                    throw new ArgumentException("Invalid enemy participants."); // 잘못된 적 구성 차단
                } // 적 오류 끝
                copy.Add(enemy); // 검증된 적 참가자 보관
            } // 참가자 검사 끝
            this.player = player; // 플레이어 참조 연결
            this.enemies = copy.AsReadOnly(); // 외부에서 변경할 수 없는 적 목록 연결
        } // 구성 끝
        public bool TrySelectEnemy(BattleActor enemy) // 전투 중 생존 적 선택
        { // 선택 시작
            if (selected != null && !selected.IsAlive) // 이전 선택 대상의 사망 검사
            { // 이전 대상 정리 시작
                selected = null; // 사망한 대상의 선택 해제
            } // 이전 대상 정리 끝
            if (!player.IsAlive || enemy == null || !enemy.IsAlive) // 플레이어와 새 대상의 생존 검사
            { // 선택 거절 시작
                return false; // 사망자 또는 빈 대상 선택 차단
            } // 선택 거절 끝
            foreach (var candidate in enemies) // 실제 전투 참가자 순회
            { // 참가자 비교 시작
                if (ReferenceEquals(candidate, enemy)) // 정확히 같은 참가자 검사
                { // 선택 확정 시작
                    selected = candidate; // 명시적으로 선택한 대상 보관
                    return true; // 생존 적 선택 성공
                } // 선택 확정 끝
            } // 참가자 비교 끝
            return false; // 전투 밖 대상 선택 차단
        } // 선택 끝
    } // 클래스 끝
} // 이름 공간 끝
