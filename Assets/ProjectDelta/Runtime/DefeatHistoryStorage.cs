using System; // 저장 오류와 고유 식별자
using System.Collections.Generic; // 패배 기록 목록
using System.IO; // 독점 잠금과 원자적 파일 교체
using System.Security.Cryptography; // 저장 무결성 체크섬
using System.Text; // UTF8 저장 인코딩
using ProjectDelta.Core; // 실제 패배 원인
using UnityEngine; // Unity 직렬화
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DefeatHistoryEntry // 검증된 영구 패배 기록 한 건
    { // 클래스 시작
        public string TransactionId { get; } // 회차 종료 고유 식별자
        public string TargetId { get; } // 확인된 패배 기록 대상
        public BattleDefeatReason Reason { get; } // 확인된 패배 원인
        internal DefeatHistoryEntry(string transactionId, string targetId, BattleDefeatReason reason) // 검증된 기록 구성
        { // 구성 시작
            TransactionId = transactionId; // 회차 종료 식별자 보관
            TargetId = targetId; // 패배 대상 보관
            Reason = reason; // 패배 원인 보관
        } // 구성 끝
    } // 클래스 끝
    public sealed class DefeatHistoryStorage // 독립 패배 기록의 안전한 영구 저장
    { // 클래스 시작
        [Serializable] // Unity 직렬화 본문 표시
        private sealed class Entry // 파일 내부 패배 기록
        { // 클래스 시작
            public string transactionId; // 회차 종료 고유 식별자
            public string targetId; // 패배 기록 대상 식별자
            public int reason; // 패배 원인 숫자
        } // 클래스 끝
        [Serializable] // Unity 직렬화 본문 표시
        private sealed class Payload // 체크섬으로 보호할 기록 본문
        { // 클래스 시작
            public int version; // 기록 형식 버전
            public Entry[] entries; // 영구 패배 기록 목록
        } // 클래스 끝
        [Serializable] // Unity 직렬화 외피 표시
        private sealed class Envelope // 본문과 체크섬 외피
        { // 클래스 시작
            public string payload; // 직렬화한 기록 본문
            public string checksum; // 기록 본문의 SHA256 체크섬
        } // 클래스 끝
        private readonly string root; // 영구 기록 저장 폴더
        private readonly string current; // 패배 기록 현재 파일
        private readonly Func<string, bool> validTarget; // 실제 등록 대상 검사
        private string expectedHash; // 마지막으로 읽은 파일의 해시
        private bool loaded; // 최초 검증 완료 상태
        private bool blocked; // 손상 또는 동시 변경 보호 상태
        public bool IsBlocked => blocked; // 쓰기 보호 상태 조회
        public static string ResolveDirectory() // 기존 2D 저장 폴더 조회
        { // 조회 시작
            return ObservationStorage.ResolveDirectory(); // 관찰 기록과 같은 폴더의 별도 파일
        } // 조회 끝
        public DefeatHistoryStorage(string directory, Func<string, bool> validTarget) // 저장 경로와 대상 검증 연결
        { // 연결 시작
            root = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); // 절대 저장 경로 보관
            current = Path.Combine(root, "defeat-v1.json"); // 기존 파일과 분리된 패배 기록
            this.validTarget = validTarget ?? throw new ArgumentNullException(nameof(validTarget)); // 등록 대상 검사 연결
        } // 연결 끝
        public IReadOnlyList<DefeatHistoryEntry> Load(out string warning) // 검증된 패배 기록 조회
        { // 조회 시작
            warning = string.Empty; // 기본 오류 안내 없음
            loaded = true; // 최초 파일 검사 시작
            blocked = false; // 이전 검사 보호 상태 초기화
            expectedHash = null; // 마지막 파일 해시 초기화
            try // 현재 파일과 복구 후보 검사
            { // 검사 시작
                if (File.Exists(current)) // 현재 패배 파일 존재 검사
                { // 현재 파일 검사 시작
                    if (TryRead(current, out var records, out var hash)) // 현재 파일 무결성 검사
                    { // 정상 파일 시작
                        expectedHash = hash; // 동시 변경 검사 기준 보관
                        return records.AsReadOnly(); // 검증된 기록 전달
                    } // 정상 파일 끝
                    blocked = true; // 손상 원본의 덮어쓰기 차단
                } // 현재 파일 검사 끝
                if (Directory.Exists(root)) // 복구 후보 폴더 검사
                { // 복구 후보 검사 시작
                    var candidates = new List<string>(Directory.GetFiles(root, "defeat-*.tmp")); // 중단된 저장 후보
                    candidates.AddRange(Directory.GetFiles(root, "defeat-backup-*.json")); // 정상 기록의 백업 후보
                    candidates.Sort((left, right) => // 복구 후보의 저장 시도 순서
                    { // 정렬 시작
                        var order = CandidateTicks(right).CompareTo(CandidateTicks(left)); // 저장 시각 기준 최신 우선
                        return order != 0 ? order : string.CompareOrdinal(right, left); // 같은 시각의 고정 순서
                    }); // 정렬 끝
                    foreach (var candidate in candidates) // 복구 후보 순회
                    { // 복구 후보 검사 시작
                        if (TryRead(candidate, out var recovered, out _)) // 후보의 체크섬과 대상 검사
                        { // 복구 가능 시작
                            blocked = true; // 원본과 복구 파일의 자동 변경 차단
                            warning = "패배 기록의 복구 파일을 읽었습니다. 원본 확인 전에는 저장할 수 없습니다."; // 복구 상태 안내
                            return recovered.AsReadOnly(); // 읽기 전용 복구 기록
                        } // 복구 가능 끝
                    } // 복구 후보 검사 끝
                    blocked = blocked || candidates.Count > 0; // 손상된 복구 후보 보존
                } // 복구 후보 검사 끝
                if (blocked) // 손상 파일이나 복구 후보 존재 검사
                { // 손상 안내 시작
                    warning = "패배 기록을 읽을 수 없습니다. 기존 파일을 보존합니다."; // 손상과 파일 보존 안내
                } // 손상 안내 끝
                return new List<DefeatHistoryEntry>().AsReadOnly(); // 새 기록 또는 읽기 실패의 빈 결과
            } // 검사 끝
            catch (Exception) // 경로 또는 파일 조회 실패
            { // 조회 실패 시작
                blocked = true; // 접근할 수 없는 기록 보호
                warning = "패배 기록을 읽을 수 없습니다. 기존 파일을 보존합니다."; // 파일 접근 오류 안내
                return new List<DefeatHistoryEntry>().AsReadOnly(); // 실패한 저장의 빈 결과
            } // 조회 실패 끝
        } // 조회 끝
        public bool TryAppend(string transactionId, string targetId, BattleDefeatReason reason, out bool duplicate, out string error) // 한 회차 패배 기록의 안전한 추가
        { // 추가 시작
            duplicate = false; // 기본 중복 없음
            error = string.Empty; // 기본 오류 없음
            if (!loaded || blocked || !IsValidRecord(transactionId, targetId, reason)) // 최초 읽기와 기록 유효성 검사
            { // 추가 거절 시작
                error = "패배 기록을 저장할 수 없습니다. 대상과 기존 파일을 확인해 주세요."; // 유효하지 않은 저장 안내
                return false; // 기존 기록 보존
            } // 추가 거절 끝
            var replaced = false; // 현재 파일 교체 여부
            try // 저장 잠금과 원자적 교체
            { // 저장 시작
                Directory.CreateDirectory(root); // 독립 저장 폴더 준비
                using (var gate = new FileStream(Path.Combine(root, "defeat.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 다른 실행과 독점 저장 잠금
                { // 잠금 시작
                    var exists = File.Exists(current); // 현재 파일 존재 확인
                    if (exists != (expectedHash != null)) // 마지막 읽기 이후 생성 또는 삭제 검사
                    { // 동시 변경 시작
                        blocked = true; // 다른 실행의 파일 보존
                        error = "패배 기록이 다른 실행에서 변경되었습니다."; // 동시 변경 안내
                        return false; // 오래된 상태의 저장 차단
                    } // 동시 변경 끝
                    var records = new List<DefeatHistoryEntry>(); // 현재 기록의 독립 목록
                    if (exists && (!TryRead(current, out records, out var actualHash) || actualHash != expectedHash)) // 현재 파일 무결성과 변경 검사
                    { // 변경 또는 손상 시작
                        blocked = true; // 변경되거나 손상된 파일 보호
                        error = "패배 기록이 변경되었거나 손상되었습니다."; // 파일 오류 안내
                        return false; // 현재 파일 덮어쓰기 차단
                    } // 변경 또는 손상 끝
                    foreach (var existing in records) // 기존 회차 종료 기록 순회
                    { // 중복 검사 시작
                        if (existing.TransactionId != transactionId) // 다른 회차 종료 검사
                        { // 다른 기록 시작
                            continue; // 다음 회차 종료 검사
                        } // 다른 기록 끝
                        if (existing.TargetId != targetId || existing.Reason != reason) // 같은 종료의 서로 다른 결과 검사
                        { // 불일치 시작
                            error = "같은 회차 종료의 패배 기록이 다릅니다."; // 중복 식별자 충돌 안내
                            return false; // 기존 결과 보존
                        } // 불일치 끝
                        duplicate = true; // 같은 종료와 같은 결과 확인
                        return true; // 재시도의 추가 기록 방지
                    } // 중복 검사 끝
                    if (records.Count >= 4096) // 기록 형식의 최대 수 검사
                    { // 기록 수 제한 시작
                        error = "패배 기록의 저장 가능한 수를 초과했습니다."; // 기록 한도 안내
                        return false; // 유효하지 않은 임시 파일 생성 차단
                    } // 기록 수 제한 끝
                    records.Add(new DefeatHistoryEntry(transactionId, targetId, reason)); // 새로운 패배 결과 추가
                    var entries = new Entry[records.Count]; // 저장할 직렬화 기록 배열
                    for (var index = 0; index < records.Count; index++) // 검증된 기록 순회
                    { // 직렬화 준비 시작
                        entries[index] = new Entry { transactionId = records[index].TransactionId, targetId = records[index].TargetId, reason = (int)records[index].Reason }; // 검증된 저장 항목 생성
                    } // 직렬화 준비 끝
                    var payload = JsonUtility.ToJson(new Payload { version = 1, entries = entries }); // 저장할 버전과 기록 본문
                    var envelope = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(Encoding.UTF8.GetBytes(payload)) }); // 본문의 체크섬 포함 외피
                    var transaction = DateTime.UtcNow.Ticks.ToString("D19") + "-" + Guid.NewGuid().ToString("N"); // 충돌 없는 저장 시도 식별자
                    var temp = Path.Combine(root, "defeat-" + transaction + ".tmp"); // 같은 폴더의 임시 저장 파일
                    using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 임시 파일의 독립 생성
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // UTF8 저장 문자 쓰기
                    { // 임시 파일 쓰기 시작
                        writer.Write(envelope); // 체크섬 포함 내용 기록
                        writer.Flush(); // 문자 버퍼 기록
                        stream.Flush(true); // 저장 장치 반영 요청
                    } // 임시 파일 쓰기 끝
                    if (!TryRead(temp, out var stagedRecords, out var stagedHash) || stagedRecords.Count != records.Count || stagedHash != Hash(Encoding.UTF8.GetBytes(envelope))) // 임시 파일 재읽기와 원본 동일성 검사
                    { // 임시 검증 실패 시작
                        error = "패배 기록의 임시 저장을 검증하지 못했습니다."; // 임시 파일 오류 안내
                        return false; // 현재 파일 보존
                    } // 임시 검증 실패 끝
                    if (exists) // 기존 정상 기록 존재 검사
                    { // 백업과 교체 시작
                        var backup = Path.Combine(root, "defeat-backup-" + transaction + ".json"); // 이전 정상 기록의 고유 백업
                        File.Copy(current, backup, false); // 기존 파일의 별도 백업
                        File.Replace(temp, current, null); // 현재 기록의 원자적 교체
                    } // 백업과 교체 끝
                    else // 첫 기록 분기
                    { // 첫 저장 시작
                        File.Move(temp, current); // 없는 현재 파일에 임시 기록 이동
                    } // 첫 저장 끝
                    replaced = true; // 현재 파일 교체 완료 표시
                    if (!TryRead(current, out _, out var writtenHash) || writtenHash != stagedHash) // 교체한 파일과 임시 파일의 동일성 검사
                    { // 최종 검증 실패 시작
                        blocked = true; // 추가 쓰기 차단과 복구 파일 보존
                        error = "저장한 패배 기록을 검증하지 못했습니다."; // 최종 검증 오류 안내
                        return false; // 검증되지 않은 기록의 성공 차단
                    } // 최종 검증 실패 끝
                    expectedHash = writtenHash; // 다음 동시 변경 검사 기준 갱신
                    return true; // 안전하게 검증한 기록 저장 성공
                } // 잠금 끝
            } // 저장 끝
            catch (Exception) // 잠금과 파일 접근 실패
            { // 저장 실패 시작
                blocked = blocked || replaced; // 교체 후 오류의 추가 쓰기 차단
                error = "패배 기록 저장에 실패했습니다. 저장 공간과 접근 권한을 확인해 주세요."; // 실제 저장 실패 안내
                return false; // 저장 실패와 원본 보존
            } // 저장 실패 끝
        } // 추가 끝
        private bool IsValidRecord(string transactionId, string targetId, BattleDefeatReason reason) // 패배 기록의 식별자와 원인 검사
        { // 검사 시작
            if (!ValidId(transactionId) || !ValidId(targetId) || (reason != BattleDefeatReason.HealthZero && reason != BattleDefeatReason.Surrender)) // 식별자와 원인 범위 검사
            { // 형식 오류 시작
                return false; // 잘못된 기록 차단
            } // 형식 오류 끝
            try // 등록 대상 검사
            { // 대상 검사 시작
                return validTarget(targetId); // 외부 등록 대상 목록의 실제 검증
            } // 대상 검사 끝
            catch (Exception) // 대상 검증 실패
            { // 검증 오류 시작
                return false; // 검증되지 않은 상대 기록 차단
            } // 검증 오류 끝
        } // 검사 끝
        private static long CandidateTicks(string path) // 복구 후보 이름의 저장 시각 조회
        { // 시각 조회 시작
            var name = Path.GetFileName(path); // 후보 파일 이름
            var prefix = name.StartsWith("defeat-backup-", StringComparison.Ordinal) ? "defeat-backup-" : "defeat-"; // 백업 또는 임시 파일 접두사
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length < prefix.Length + 19) // 저장 시각 자리 검사
            { // 시각 누락 시작
                return 0; // 알 수 없는 시각의 낮은 우선순위
            } // 시각 누락 끝
            return long.TryParse(name.Substring(prefix.Length, 19), out var ticks) ? ticks : 0; // 저장 시각 또는 기본 순서
        } // 시각 조회 끝
        private static bool ValidId(string value) // 안전한 식별자 형식 검사
        { // 검사 시작
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Trim() != value) // 빈 값과 길이 및 외부 공백 검사
            { // 형식 오류 시작
                return false; // 유효하지 않은 식별자 차단
            } // 형식 오류 끝
            foreach (var character in value) // 식별자 문자 순회
            { // 문자 검사 시작
                if (char.IsControl(character)) // 제어 문자 검사
                { // 제어 문자 시작
                    return false; // 잘못된 저장 식별자 차단
                } // 제어 문자 끝
            } // 문자 검사 끝
            return true; // 저장 가능한 식별자 확인
        } // 검사 끝
        private bool TryRead(string path, out List<DefeatHistoryEntry> records, out string hash) // 체크섬과 기록 내용 검증
        { // 읽기 시작
            records = null; // 기본 기록 없음
            hash = null; // 기본 파일 해시 없음
            try // 파일과 직렬화 검사
            { // 검사 시작
                var info = new FileInfo(path); // 저장 파일 크기 조회
                if (!info.Exists || info.Length > 1024 * 1024) // 현재 파일과 최대 크기 검사
                { // 파일 크기 오류 시작
                    return false; // 잘못된 파일 거절
                } // 파일 크기 오류 끝
                var bytes = File.ReadAllBytes(path); // 저장 파일 전체 읽기
                var envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(bytes)); // 체크섬 외피 해석
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum) || Hash(Encoding.UTF8.GetBytes(envelope.payload)) != envelope.checksum) // 본문과 체크섬 검사
                { // 무결성 오류 시작
                    return false; // 손상된 기록 거절
                } // 무결성 오류 끝
                var payload = JsonUtility.FromJson<Payload>(envelope.payload); // 패배 기록 본문 해석
                if (payload == null || payload.version != 1 || payload.entries == null || payload.entries.Length > 4096) // 버전과 기록 수 검사
                { // 형식 오류 시작
                    return false; // 잘못된 저장 형식 거절
                } // 형식 오류 끝
                var ids = new HashSet<string>(StringComparer.Ordinal); // 회차 종료 식별자 중복 검사
                var result = new List<DefeatHistoryEntry>(); // 검증한 패배 기록 목록
                foreach (var entry in payload.entries) // 저장한 기록 순회
                { // 기록 검사 시작
                    if (entry == null || !IsValidRecord(entry.transactionId, entry.targetId, (BattleDefeatReason)entry.reason) || !ids.Add(entry.transactionId)) // 대상과 원인 및 종료 식별자 검사
                    { // 잘못된 기록 시작
                        return false; // 손상되거나 중복된 기록 차단
                    } // 잘못된 기록 끝
                    result.Add(new DefeatHistoryEntry(entry.transactionId, entry.targetId, (BattleDefeatReason)entry.reason)); // 검증한 기록 보관
                } // 기록 검사 끝
                records = result; // 검증된 전체 기록 전달
                hash = Hash(bytes); // 동시 변경 검사 기준 계산
                return true; // 검증된 파일 읽기 성공
            } // 검사 끝
            catch (Exception) // 파일 접근 또는 형식 오류
            { // 읽기 오류 시작
                return false; // 손상된 파일 거절
            } // 읽기 오류 끝
        } // 읽기 끝
        private static string Hash(byte[] bytes) // 저장 파일의 SHA256 해시
        { // 계산 시작
            using (var algorithm = SHA256.Create()) // 해시 계산 객체 생성
            { // 계산 객체 시작
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty); // 대문자 체크섬 문자열
            } // 계산 객체 끝
        } // 계산 끝
    } // 클래스 끝
} // 이름 공간 끝
