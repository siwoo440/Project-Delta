using System; // 직렬화와 식별자 검사
using System.Collections.Generic; // 고유 관계와 수령 기록 검사
namespace ProjectDelta.Core // 독립 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum NpcRelationshipStage // 기획된 호감도 단계
    { // 단계 목록 시작
        Neutral, // 중립 단계
        Interest, // 관심 단계
        Trust, // 신뢰 단계
        Special, // 특별 단계
        EndingAvailable // 엔딩 가능 단계
    } // 단계 목록 끝
    [Serializable] // NPC별 호감도의 직렬화 형식
    public sealed class NpcRelationshipRecord // 단일 NPC의 지속 관계
    { // 클래스 시작
        public string npcId; // 고유 NPC 식별자
        public int affection; // 영부터 백까지의 호감도
    } // 클래스 끝
    [Serializable] // 회차별 우호 수령의 직렬화 형식
    public sealed class NpcRelationshipReceipt // 회차와 NPC별 단일 수령 기록
    { // 클래스 시작
        public string runId; // 실제 회차 식별자
        public string npcId; // 실제 NPC 식별자
    } // 클래스 끝
    [Serializable] // 관계와 수령 목록의 직렬화 형식
    public sealed class NpcRelationshipSaveData // 전체 NPC 관계 저장본
    { // 클래스 시작
        public NpcRelationshipRecord[] relationships; // 고유 NPC별 호감도 목록
        public NpcRelationshipReceipt[] receipts; // 회차와 NPC별 수령 기록
    } // 클래스 끝
    public static class NpcRelationshipRules // 기획 단계와 시험 우호 수치
    { // 클래스 시작
        public const int TrialFriendlyGain = 5; // 미확정 시험 우호 선택 증가량
        public static bool IsValidId(string id) // 안정적인 저장 식별자 검사
        { // 검사 시작
            if (string.IsNullOrEmpty(id) || id.Length > 128) // 길이와 누락 검사
            { // 오류 시작
                return false; // 빈 식별자와 과도한 길이 차단
            } // 오류 끝
            foreach (var character in id) // 식별자 문자 순회
            { // 문자 검사 시작
                if (char.IsWhiteSpace(character) || char.IsControl(character)) // 공백과 제어문자 검사
                { // 오류 시작
                    return false; // 혼동되는 식별자 차단
                } // 오류 끝
            } // 문자 검사 끝
            return true; // 유효한 식별자
        } // 검사 끝
        public static NpcRelationshipStage Stage(int affection) // 호감도의 기획 단계 조회
        { // 조회 시작
            if (affection < 0 || affection > 100) // 실제 호감도 범위 검사
            { // 오류 시작
                throw new ArgumentOutOfRangeException(nameof(affection)); // 잘못된 단계 입력 차단
            } // 오류 끝
            if (affection <= 33) // 중립 구간 검사
            { // 중립 시작
                return NpcRelationshipStage.Neutral; // 중립 단계
            } // 중립 끝
            if (affection <= 66) // 관심 구간 검사
            { // 관심 시작
                return NpcRelationshipStage.Interest; // 관심 단계
            } // 관심 끝
            if (affection <= 84) // 신뢰 구간 검사
            { // 신뢰 시작
                return NpcRelationshipStage.Trust; // 신뢰 단계
            } // 신뢰 끝
            if (affection <= 99) // 특별 구간 검사
            { // 특별 시작
                return NpcRelationshipStage.Special; // 특별 단계
            } // 특별 끝
            return NpcRelationshipStage.EndingAvailable; // 백 호감도의 엔딩 가능
        } // 조회 끝
    } // 클래스 끝
    public sealed class NpcRelationshipState // 외부 변경을 막는 관계와 수령 상태
    { // 클래스 시작
        private readonly NpcRelationshipSaveData data; // 독립 관계 저장 데이터
        private NpcRelationshipState(NpcRelationshipSaveData saved) // 검증된 상태 생성
        { // 생성 시작
            data = Copy(saved); // 외부 객체와 분리
        } // 생성 끝
        public static NpcRelationshipState Empty => new NpcRelationshipState(new NpcRelationshipSaveData { relationships = new NpcRelationshipRecord[0], receipts = new NpcRelationshipReceipt[0] }); // 빈 관계와 수령 기록
        public static bool TryRestore(NpcRelationshipSaveData saved, out NpcRelationshipState state) // 저장 관계와 수령 기록 검증
        { // 복원 시작
            state = null; // 기본 복원 실패
            if (saved == null || saved.relationships == null || saved.receipts == null) // 저장 배열 존재 검사
            { // 오류 시작
                return false; // 누락된 저장 데이터 차단
            } // 오류 끝
            var npcs = new HashSet<string>(StringComparer.Ordinal); // 관계별 고유 NPC 식별자
            foreach (var relationship in saved.relationships) // 실제 관계 순회
            { // 관계 검사 시작
                if (relationship == null || !NpcRelationshipRules.IsValidId(relationship.npcId) || relationship.affection < 0 || relationship.affection > 100 || !npcs.Add(relationship.npcId)) // 식별자와 호감도 및 중복 검사
                { // 오류 시작
                    return false; // 손상되거나 중복된 관계 차단
                } // 오류 끝
            } // 관계 검사 끝
            var receipts = new HashSet<(string, string)>(); // 고유 회차와 NPC 조합
            foreach (var receipt in saved.receipts) // 실제 수령 기록 순회
            { // 수령 검사 시작
                if (receipt == null || !NpcRelationshipRules.IsValidId(receipt.runId) || !NpcRelationshipRules.IsValidId(receipt.npcId) || !npcs.Contains(receipt.npcId) || !receipts.Add((receipt.runId, receipt.npcId))) // 식별자와 관계 및 중복 수령 검사
                { // 오류 시작
                    return false; // 누락 관계와 중복 수령 차단
                } // 오류 끝
            } // 수령 검사 끝
            state = new NpcRelationshipState(saved); // 검증된 독립 상태
            return true; // 정상 복원 성공
        } // 복원 끝
        public NpcRelationshipSaveData Snapshot() // 저장할 독립 관계 조회
        { // 조회 시작
            return Copy(data); // 관계와 수령의 심층 복사
        } // 조회 끝
        public int GetAffection(string npcId) // 특정 NPC 호감도 조회
        { // 조회 시작
            if (!NpcRelationshipRules.IsValidId(npcId)) // 식별자 검사
            { // 오류 시작
                throw new ArgumentException("Invalid NPC ID.", nameof(npcId)); // 잘못된 조회 차단
            } // 오류 끝
            foreach (var record in data.relationships) // 저장된 관계 순회
            { // 일치 검사 시작
                if (record.npcId == npcId) // 실제 NPC 검사
                { // 일치 시작
                    return record.affection; // 저장된 호감도 전달
                } // 일치 끝
            } // 일치 검사 끝
            return 0; // 처음 만나는 NPC의 기본 호감도
        } // 조회 끝
        public bool HasReceipt(string runId, string npcId) // 해당 회차의 우호 수령 확인
        { // 확인 시작
            if (!NpcRelationshipRules.IsValidId(runId) || !NpcRelationshipRules.IsValidId(npcId)) // 조회 식별자 검사
            { // 오류 시작
                return false; // 잘못된 조회 거절
            } // 오류 끝
            foreach (var receipt in data.receipts) // 실제 수령 기록 순회
            { // 일치 검사 시작
                if (receipt.runId == runId && receipt.npcId == npcId) // 회차와 NPC 일치 검사
                { // 일치 시작
                    return true; // 이미 적용한 우호 선택
                } // 일치 끝
            } // 일치 검사 끝
            return false; // 아직 적용하지 않은 선택
        } // 확인 끝
        public bool TryGrantFriendliness(string runId, string npcId, out NpcRelationshipState state) // 회차별 한 번의 시험 우호 선택
        { // 적용 시작
            state = null; // 기본 적용 실패
            if (!NpcRelationshipRules.IsValidId(runId) || !NpcRelationshipRules.IsValidId(npcId) || HasReceipt(runId, npcId)) // 식별자와 중복 수령 검사
            { // 오류 시작
                return false; // 잘못된 식별자와 재적용 차단
            } // 오류 끝
            var next = Copy(data); // 원본을 보존한 독립 상태
            var index = Array.FindIndex(next.relationships, record => record.npcId == npcId); // 기존 관계 위치
            if (index < 0) // 처음 만나는 NPC 검사
            { // 신규 관계 시작
                index = next.relationships.Length; // 새 관계 위치
                Array.Resize(ref next.relationships, index + 1); // 새 NPC 공간 확보
                next.relationships[index] = new NpcRelationshipRecord { npcId = npcId }; // 기본 호감도의 새 관계
            } // 신규 관계 끝
            next.relationships[index].affection = Math.Min(100, next.relationships[index].affection + NpcRelationshipRules.TrialFriendlyGain); // 최대 백을 지키는 시험 증가
            var receiptIndex = next.receipts.Length; // 새 수령 기록 위치
            Array.Resize(ref next.receipts, receiptIndex + 1); // 단일 수령 기록 공간
            next.receipts[receiptIndex] = new NpcRelationshipReceipt { runId = runId, npcId = npcId }; // 회차별 고유 수령 기록
            state = new NpcRelationshipState(next); // 원자적으로 저장할 관계와 수령 상태
            return true; // 유효한 우호 선택 준비
        } // 적용 끝
        private static NpcRelationshipSaveData Copy(NpcRelationshipSaveData source) // 관계와 수령 배열의 심층 복사
        { // 복사 시작
            var relationships = new NpcRelationshipRecord[source.relationships.Length]; // 독립 관계 배열
            for (var index = 0; index < relationships.Length; index++) // 관계 순회
            { // 관계 복사 시작
                var item = source.relationships[index]; // 현재 NPC 관계
                relationships[index] = new NpcRelationshipRecord { npcId = item.npcId, affection = item.affection }; // 개별 관계 복사
            } // 관계 복사 끝
            var receipts = new NpcRelationshipReceipt[source.receipts.Length]; // 독립 수령 배열
            for (var index = 0; index < receipts.Length; index++) // 수령 순회
            { // 수령 복사 시작
                var item = source.receipts[index]; // 현재 수령 기록
                receipts[index] = new NpcRelationshipReceipt { runId = item.runId, npcId = item.npcId }; // 개별 수령 복사
            } // 수령 복사 끝
            return new NpcRelationshipSaveData { relationships = relationships, receipts = receipts }; // 독립 저장본 전달
        } // 복사 끝
    } // 클래스 끝
} // 이름 공간 끝
