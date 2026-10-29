using System; // 저장 형식과 안전한 계산
using System.Collections.Generic; // 참가자 식별자 검사
namespace ProjectDelta.Core // 독립 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // 개별 적의 저장 형식
    public sealed class TrialEventBattleEnemy // 일반 전투에서 넘겨받은 개별 적
    { // 클래스 시작
        public string id; // 전투 내 고유 참가자 식별자
        public int slot; // 원래 일반 전투의 적 슬롯
        public int health; // 현재 생존 체력
        public int charm; // 주도권 판정 매력
        public int progress; // 개별 진행 게이지
        public int status; // 참여 영과 진행 완료 일 및 이탈 이와 체력 소진 삼
    } // 클래스 끝
    [Serializable] // 단일 진행의 저장 형식
    public sealed class TrialEventBattleStep // 입력과 난수 재생 기록
    { // 클래스 시작
        public int kind; // 행동 영과 체력 변경 일 및 이탈 이
        public int target; // 원래 참가자 배열의 적 번호
        public int action; // 관찰 영과 시험 행동 일에서 삼
        public int playerRoll; // 플레이어 주도권 주사위
        public int enemyRoll; // 상대 주도권 주사위
        public int healthAfter; // 체력 변경 후 값
    } // 클래스 끝
    [Serializable] // 이벤트 전투의 전체 저장 형식
    public sealed class TrialEventBattleSaveData // 최초 스냅샷과 결정된 진행 기록
    { // 클래스 시작
        public TrialEventBattleEnemy[] initialEnemies; // 일반 전투의 최초 생존 적
        public int initialStamina; // 최초 플레이어 정력
        public int playerCharm; // 플레이어 주도권 매력
        public int normalExperience; // 진입 생존 적의 원래 경험치
        public int priorExperience; // 일반 전투에서 이미 처치한 적의 경험치
        public TrialEventBattleStep[] steps; // 순서가 고정된 행동과 난수
        public TrialEventBattleEnemy[] enemies; // 재생 검증할 현재 적 상태
        public int stamina; // 현재 플레이어 정력
        public int turn; // 플레이어 영하 일 또는 상대 번호
        public int outcome; // 진행 영과 승리 일 및 패배 이
        public int experience; // 승리 시 시험 경험치
    } // 클래스 끝
    public static class TrialEventBattleRules // 불명 수치를 분리한 시험 전투 규칙
    { // 클래스 시작
        public const int MaxSteps = 256; // 재생 검증을 제한하는 최대 기록 수
        public static int TrialProgress(int action) // 관찰과 시험 행동의 진행량
        { // 조회 시작
            switch (action) // 행동 번호 검사
            { // 번호 선택 시작
                case 0: return 0; // 관찰의 진행 없음
                case 1: return 10; // 시험 첫 행동 진행량
                case 2: return 20; // 시험 둘째 행동 진행량
                case 3: return 30; // 시험 셋째 행동 진행량
                default: return -1; // 알 수 없는 행동
            } // 번호 선택 끝
        } // 조회 끝
        public static int TrialStaminaCost(int action) // 시험 행동의 플레이어 정력 비용
        { // 조회 시작
            switch (action) // 행동 번호 검사
            { // 번호 선택 시작
                case 0: return 0; // 관찰의 정력 비용 없음
                case 1: return 3; // 시험 첫 행동 비용
                case 2: return 5; // 시험 둘째 행동 비용
                case 3: return 8; // 시험 셋째 행동 비용
                default: return -1; // 알 수 없는 행동
            } // 번호 선택 끝
        } // 조회 끝
        public static int VictoryExperience(int normalExperience) // 생존 적의 시험 승리 경험치
        { // 계산 시작
            return VictoryExperience(normalExperience, 0); // 기존 처치 경험치 없는 계산
        } // 계산 끝
        public static int VictoryExperience(int normalExperience, int priorExperience) // 생존 적 팔십 퍼센트와 기존 처치 경험치
        { // 계산 시작
            if (normalExperience < 0 || priorExperience < 0) // 두 경험치의 음수 검사
            { // 오류 시작
                return -1; // 잘못된 경험치 차단
            } // 오류 끝
            var total = (long)normalExperience * 80 / 100 + priorExperience; // 생존 대상 버림과 기존 처치 합산
            return total > int.MaxValue ? -1 : (int)total; // 정수 초과 결과 차단
        } // 계산 끝
    } // 클래스 끝
    public sealed class TrialEventBattleState // 재생 검증되는 불변 이벤트 전투
    { // 클래스 시작
        private readonly TrialEventBattleSaveData data; // 외부 변경을 막는 전투 기록
        private TrialEventBattleState(TrialEventBattleSaveData value) // 검증된 독립 상태 생성
        { // 생성 시작
            data = Copy(value); // 전체 전투 데이터 복사
        } // 생성 끝
        public static bool TryCreate(TrialEventBattleEnemy[] enemies, int stamina, int playerCharm, int normalExperience, out TrialEventBattleState state) // 이전 처치가 없는 이벤트 전투 생성
        { // 생성 시작
            return TryCreate(enemies, stamina, playerCharm, normalExperience, 0, out state); // 기본 기존 처치 경험치
        } // 생성 끝
        public static bool TryCreate(TrialEventBattleEnemy[] enemies, int stamina, int playerCharm, int normalExperience, int priorExperience, out TrialEventBattleState state) // 생존 적과 기존 처치 경험치로 전투 생성
        { // 생성 시작
            state = null; // 기본 생성 실패
            if (enemies == null || enemies.Length < 1 || enemies.Length > 3 || stamina <= 0 || playerCharm < 0 || TrialEventBattleRules.VictoryExperience(normalExperience, priorExperience) < 0) // 참가 인원과 자원 및 경험치 범위 검사
            { // 오류 시작
                return false; // 잘못된 시작 조건 차단
            } // 오류 끝
            var ids = new HashSet<string>(StringComparer.Ordinal); // 전투 내 고유 참가자
            var slots = new HashSet<int>(); // 원래 전투의 고유 슬롯
            foreach (var enemy in enemies) // 최초 생존 적 순회
            { // 적 검사 시작
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.id) || enemy.id.Length > 128 || !ids.Add(enemy.id) || enemy.slot < 0 || enemy.slot > 2 || !slots.Add(enemy.slot) || enemy.health <= 0 || enemy.charm < 0 || enemy.progress != 0 || enemy.status != 0) // 생존 상태와 고유 식별자 검사
                { // 오류 시작
                    return false; // 중복과 잘못된 초기 적 차단
                } // 오류 끝
            } // 적 검사 끝
            var copy = CopyEnemies(enemies); // 원본 일반 전투와 분리
            state = new TrialEventBattleState(new TrialEventBattleSaveData { initialEnemies = copy, initialStamina = stamina, playerCharm = playerCharm, normalExperience = normalExperience, priorExperience = priorExperience, steps = new TrialEventBattleStep[0], enemies = CopyEnemies(copy), stamina = stamina, turn = -1 }); // 플레이어 선공의 독립 상태
            return true; // 정상 전투 생성
        } // 생성 끝
        public static bool TryRestore(TrialEventBattleSaveData saved, out TrialEventBattleState state) // 최초 입력부터 전체 진행 재생 검증
        { // 복원 시작
            state = null; // 기본 복원 실패
            if (saved == null || saved.steps == null || saved.steps.Length > TrialEventBattleRules.MaxSteps || saved.enemies == null || !TryCreate(saved.initialEnemies, saved.initialStamina, saved.playerCharm, saved.normalExperience, saved.priorExperience, out var current)) // 최초 참가자와 로그 검사
            { // 오류 시작
                return false; // 누락된 시작 정보 차단
            } // 오류 끝
            foreach (var step in saved.steps) // 순서대로 기록 재생
            { // 재생 시작
                if (step == null || !current.TryStep(step, out var next)) // 각 행동과 주사위 검사
                { // 오류 시작
                    return false; // 위조되거나 잘못된 행동 차단
                } // 오류 끝
                current = next; // 검증된 다음 상태
            } // 재생 끝
            var actual = current.data; // 재생한 실제 결과
            if (saved.stamina != actual.stamina || saved.turn != actual.turn || saved.outcome != actual.outcome || saved.experience != actual.experience || !EqualEnemies(saved.enemies, actual.enemies)) // 현재 상태와 재생 결과 대조
            { // 오류 시작
                return false; // 위조된 자원과 적 및 결과 차단
            } // 오류 끝
            state = current; // 독립 재생 결과 전달
            return true; // 검증된 저장 복원
        } // 복원 끝
        public TrialEventBattleSaveData Snapshot() // 저장 가능한 독립 전투 조회
        { // 조회 시작
            return Copy(data); // 참가자와 행동 기록 심층 복사
        } // 조회 끝
        public bool TryAction(int targetIndex, int action, int playerRoll, int enemyRoll, out TrialEventBattleState state) // 행동과 양측 주도권 주사위 저장
        { // 행동 시작
            return TryStep(new TrialEventBattleStep { kind = 0, target = targetIndex, action = action, playerRoll = playerRoll, enemyRoll = enemyRoll }, out state); // 검증된 단일 행동 적용
        } // 행동 끝
        public bool TryDamageEnemy(int index, int healthAfter, out TrialEventBattleState state) // 실제 적 체력 감소 반영
        { // 체력 변경 시작
            return TryStep(new TrialEventBattleStep { kind = 1, target = index, healthAfter = healthAfter }, out state); // 검증된 체력 변경 적용
        } // 체력 변경 끝
        public bool TryRemoveEnemy(int index, out TrialEventBattleState state) // 실제 이탈한 적 제거
        { // 이탈 시작
            return TryStep(new TrialEventBattleStep { kind = 2, target = index }, out state); // 검증된 이탈 적용
        } // 이탈 끝
        private bool TryStep(TrialEventBattleStep step, out TrialEventBattleState state) // 검증 가능한 단일 상태 전이
        { // 전이 시작
            state = null; // 기본 전이 실패
            if (step == null || data.outcome != 0 || data.steps.Length >= TrialEventBattleRules.MaxSteps || step.target < 0 || step.target >= data.enemies.Length || data.enemies[step.target].status != 0) // 진행 상태와 실제 참가자 검사
            { // 오류 시작
                return false; // 종료와 제거된 적의 중복 행동 차단
            } // 오류 끝
            var next = Copy(data); // 원본을 보존한 전투 상태
            var enemy = next.enemies[step.target]; // 선택한 현재 적
            if (step.kind == 0) // 관찰 또는 행동 검사
            { // 행동 시작
                var amount = TrialEventBattleRules.TrialProgress(step.action); // 명시적인 시험 진행량
                var cost = TrialEventBattleRules.TrialStaminaCost(step.action); // 명시적인 시험 정력 비용
                if (amount < 0 || step.playerRoll < 1 || step.playerRoll > 20 || step.enemyRoll < 1 || step.enemyRoll > 20 || step.healthAfter != 0 || (next.turn >= 0 && next.turn != step.target)) // 행동과 주사위 및 현재 상대 검사
                { // 오류 시작
                    return false; // 잘못된 입력과 다른 상대 행동 차단
                } // 오류 끝
                if (next.turn == -1) // 플레이어 행동 검사
                { // 진행 시작
                    enemy.progress = Math.Min(100, enemy.progress + amount); // 해당 적의 개별 진행량
                    if (enemy.progress == 100) // 진행 완료 검사
                    { // 완료 시작
                        enemy.status = 1; // 진행 완료 적 제거
                    } // 완료 끝
                } // 진행 끝
                next.stamina = Math.Max(0, next.stamina - cost); // 각 행동의 시험 정력 소모
                if (next.stamina == 0) // 정력 소진 검사
                { // 패배 시작
                    next.outcome = 2; // 정력 소진 즉시 패배
                } // 패배 끝
                else if (AllRemoved(next.enemies)) // 모든 적 제거 검사
                { // 승리 시작
                    next.outcome = 1; // 마지막 적 제거 승리
                    next.experience = TrialEventBattleRules.VictoryExperience(next.normalExperience, next.priorExperience); // 일반 전투 경험치의 팔십 퍼센트
                } // 승리 끝
                else // 계속 진행 검사
                { // 주도권 시작
                    var opponent = enemy.status == 0 ? step.target : FirstActive(next.enemies); // 현재 적 또는 남은 적
                    var playerTotal = (long)next.playerCharm + step.playerRoll; // 플레이어 매력과 주사위
                    var enemyTotal = (long)next.enemies[opponent].charm + step.enemyRoll; // 대상 적 매력과 주사위
                    if (playerTotal > enemyTotal) // 플레이어 우위 검사
                    { // 우위 시작
                        next.turn = -1; // 다음 주도권 플레이어
                    } // 우위 끝
                    else if (playerTotal < enemyTotal) // 적 우위 검사
                    { // 우위 시작
                        next.turn = opponent; // 다음 주도권 대상 적
                    } // 우위 끝
                    else if (next.turn >= 0 && next.enemies[next.turn].status != 0) // 동점의 기존 적 제거 검사
                    { // 제거 시작
                        next.turn = -1; // 제거된 주도권의 플레이어 복구
                    } // 제거 끝
                } // 주도권 끝
            } // 행동 끝
            else if (step.kind == 1) // 외부 전투 체력 변경 검사
            { // 체력 변경 시작
                if (step.action != 0 || step.playerRoll != 0 || step.enemyRoll != 0 || step.healthAfter < 0 || step.healthAfter >= enemy.health) // 감소 체력과 불필요 입력 검사
                { // 오류 시작
                    return false; // 회복과 중복 및 잘못된 체력 차단
                } // 오류 끝
                enemy.health = step.healthAfter; // 실제 변경된 체력
                if (enemy.health == 0) // 체력 소진 검사
                { // 제거 시작
                    enemy.status = 3; // 체력 소진 적 제거
                } // 제거 끝
            } // 체력 변경 끝
            else if (step.kind == 2) // 이탈 처리 검사
            { // 이탈 시작
                if (step.action != 0 || step.playerRoll != 0 || step.enemyRoll != 0 || step.healthAfter != 0) // 이탈 시 불필요한 입력 검사
                { // 오류 시작
                    return false; // 잘못된 이탈 데이터 차단
                } // 오류 끝
                enemy.status = 2; // 실제 이탈 적 제거
            } // 이탈 끝
            else // 미등록 상태 변경
            { // 오류 시작
                return false; // 알 수 없는 행동 차단
            } // 오류 끝
            if (next.outcome == 0 && AllRemoved(next.enemies)) // 체력 소진과 이탈 뒤 전원 제거 검사
            { // 승리 시작
                next.outcome = 1; // 모든 참가자 제거 승리
                next.experience = TrialEventBattleRules.VictoryExperience(next.normalExperience, next.priorExperience); // 실제 시험 경험치
            } // 승리 끝
            if (next.outcome == 0 && next.turn >= 0 && next.enemies[next.turn].status != 0) // 주도권 대상의 외부 제거 검사
            { // 주도권 복구 시작
                next.turn = -1; // 남은 전투의 플레이어 주도권
            } // 주도권 복구 끝
            var length = next.steps.Length; // 기존 기록 수
            Array.Resize(ref next.steps, length + 1); // 새 행동 기록 공간
            next.steps[length] = CopyStep(step); // 주사위를 포함한 입력 고정
            state = new TrialEventBattleState(next); // 저장할 독립 결과
            return true; // 정상 전이 완료
        } // 전이 끝
        private static bool AllRemoved(TrialEventBattleEnemy[] enemies) // 생존 참가자 존재 검사
        { // 검사 시작
            return FirstActive(enemies) < 0; // 진행 중 적이 없을 때 승리
        } // 검사 끝
        private static int FirstActive(TrialEventBattleEnemy[] enemies) // 첫 생존 참가자 조회
        { // 조회 시작
            for (var index = 0; index < enemies.Length; index++) // 실제 적 순회
            { // 검사 시작
                if (enemies[index].status == 0) // 아직 참여 중인 적 검사
                { // 일치 시작
                    return index; // 첫 참가자 번호
                } // 일치 끝
            } // 검사 끝
            return -1; // 남은 적 없음
        } // 조회 끝
        private static bool EqualEnemies(TrialEventBattleEnemy[] first, TrialEventBattleEnemy[] second) // 저장 적 상태의 재생 결과 대조
        { // 비교 시작
            if (first == null || first.Length != second.Length) // 참가자 수 검사
            { // 오류 시작
                return false; // 누락된 적 차단
            } // 오류 끝
            for (var index = 0; index < first.Length; index++) // 실제 참가자 순회
            { // 비교 시작
                var a = first[index]; // 저장된 참가자
                var b = second[index]; // 재생된 참가자
                if (a == null || a.id != b.id || a.slot != b.slot || a.health != b.health || a.charm != b.charm || a.progress != b.progress || a.status != b.status) // 모든 참가자 값 검사
                { // 오류 시작
                    return false; // 위조된 참가자 차단
                } // 오류 끝
            } // 비교 끝
            return true; // 모든 적 상태 일치
        } // 비교 끝
        private static TrialEventBattleEnemy[] CopyEnemies(TrialEventBattleEnemy[] source) // 적 배열과 원소의 심층 복사
        { // 복사 시작
            if (source == null) // 누락 배열 검사
            { // 누락 시작
                return null; // 누락 상태 보존
            } // 누락 끝
            var result = new TrialEventBattleEnemy[source.Length]; // 독립 참가자 배열
            for (var index = 0; index < source.Length; index++) // 참가자 순회
            { // 복사 시작
                var item = source[index]; // 원본 참가자
                result[index] = item == null ? null : new TrialEventBattleEnemy { id = item.id, slot = item.slot, health = item.health, charm = item.charm, progress = item.progress, status = item.status }; // 개별 참가자 복사
            } // 복사 끝
            return result; // 독립 참가자 목록
        } // 복사 끝
        private static TrialEventBattleStep CopyStep(TrialEventBattleStep item) // 행동 입력의 독립 복사
        { // 복사 시작
            return item == null ? null : new TrialEventBattleStep { kind = item.kind, target = item.target, action = item.action, playerRoll = item.playerRoll, enemyRoll = item.enemyRoll, healthAfter = item.healthAfter }; // 모든 입력과 난수 복사
        } // 복사 끝
        private static TrialEventBattleSaveData Copy(TrialEventBattleSaveData source) // 전투와 행동 목록의 심층 복사
        { // 복사 시작
            var steps = new TrialEventBattleStep[source.steps.Length]; // 독립 행동 목록
            for (var index = 0; index < steps.Length; index++) // 실제 행동 순회
            { // 복사 시작
                steps[index] = CopyStep(source.steps[index]); // 개별 입력 복사
            } // 복사 끝
            return new TrialEventBattleSaveData { initialEnemies = CopyEnemies(source.initialEnemies), initialStamina = source.initialStamina, playerCharm = source.playerCharm, normalExperience = source.normalExperience, priorExperience = source.priorExperience, steps = steps, enemies = CopyEnemies(source.enemies), stamina = source.stamina, turn = source.turn, outcome = source.outcome, experience = source.experience }; // 모든 원본과 현재 상태 복사
        } // 복사 끝
    } // 클래스 끝
} // 이름 공간 끝
