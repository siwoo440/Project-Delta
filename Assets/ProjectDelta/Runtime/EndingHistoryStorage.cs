using System; // 저장 오류와 고유 식별자
using System.Collections.Generic; // 영구 결말 기록 목록
using System.IO; // 잠금과 원자적 파일 교체
using System.Security.Cryptography; // 저장 무결성 검사
using System.Text; // UTF8 저장 인코딩
using ProjectDelta.Core; // 확인된 결말과 최초 보상
using UnityEngine; // Unity 직렬화 도구
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class EndingHistoryEntry // 검증된 결말 거래 영수증
    { // 클래스 시작
        public string RunId { get; } // 종료된 회차 식별자
        public string TransactionId { get; } // 마왕 결과 거래 식별자
        public string EndingId { get; } // 확정된 결말 식별자
        public bool FirstUnlock { get; } // 해당 거래의 최초 해금 여부
        public int GrantedShards { get; } // 해당 거래의 실제 최초 지급량
        internal EndingHistoryEntry(string runId, string transactionId, string endingId, bool firstUnlock, int grantedShards) // 검증된 영수증 생성
        { // 생성 시작
            RunId = runId; // 회차 식별자 보관
            TransactionId = transactionId; // 마왕 거래 식별자 보관
            EndingId = endingId; // 확정 결말 보관
            FirstUnlock = firstUnlock; // 최초 해금 여부 보관
            GrantedShards = grantedShards; // 실제 지급 파편 보관
        } // 생성 끝
    } // 클래스 끝
    public sealed class EndingHistoryStorage // 결말과 최초 보상의 독립 영구 저장
    { // 클래스 시작
        [Serializable] // Unity 직렬화 형식
        private sealed class Entry // 파일 내부 결말 영수증
        { // 클래스 시작
            public string runId; // 종료된 회차 식별자
            public string transactionId; // 마왕 결과 거래 식별자
            public string endingId; // 확정된 결말 식별자
            public bool firstUnlock; // 최초 결말 해금 여부
            public int grantedShards; // 해당 거래의 최초 파편
        } // 클래스 끝
        [Serializable] // Unity 직렬화 형식
        private sealed class Payload // 체크섬으로 보호할 결말 본문
        { // 클래스 시작
            public int version; // 기록 형식 버전
            public Entry[] entries; // 순서가 고정된 결말 영수증
        } // 클래스 끝
        [Serializable] // Unity 직렬화 형식
        private sealed class Envelope // 저장 본문과 무결성 값
        { // 클래스 시작
            public string payload; // 직렬화한 결말 기록
            public string checksum; // 결말 본문의 SHA256 값
        } // 클래스 끝
        private readonly string root; // 결말 기록 저장 폴더
        private readonly string current; // 현재 결말 기록 파일
        private List<EndingHistoryEntry> entries = new List<EndingHistoryEntry>(); // 검증된 실제 결말 기록
        private string expectedHash; // 마지막으로 읽은 파일 해시
        public bool HasVerifiedCurrentFile => expectedHash != null; // 마지막으로 확인한 현재 파일 존재 여부
        private bool loaded; // 최초 조회 완료 여부
        private bool blocked; // 손상 또는 동시 변경 보호 상태
        public bool IsBlocked => blocked; // 원본 확인 전 쓰기 차단 여부
        public int TotalShards // 검증된 최초 해금 파편 합계
        { // 속성 시작
            get // 실제 파편 합계 조회
            { // 조회 시작
                var total = 0; // 최초 파편 합계
                foreach (var entry in entries) // 검증된 영수증 순회
                { // 합계 시작
                    total += entry.GrantedShards; // 실제 지급 파편 누적
                } // 합계 끝
                return total; // 현재 기록의 누적 파편 전달
            } // 조회 끝
        } // 속성 끝
        public EndingHistoryStorage(string directory) // 독립 저장 폴더 연결
        { // 연결 시작
            root = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); // 절대 저장 폴더
            current = Path.Combine(root, "ending-history-v1.json"); // 결말 전용 현재 파일
        } // 연결 끝
        public IReadOnlyList<EndingHistoryEntry> Load(out string warning) // 현재 기록과 보호된 복구 조회
        { // 조회 시작
            warning = string.Empty; // 기본 오류 안내 없음
            loaded = true; // 최초 파일 검사 시작
            blocked = false; // 새 조회의 보호 상태 초기화
            expectedHash = null; // 이전 파일 해시 초기화
            entries = new List<EndingHistoryEntry>(); // 이전 메모리 기록 초기화
            try // 현재 파일과 복구 후보 검사
            { // 파일 검사 시작
                if (File.Exists(current)) // 현재 결말 파일 존재 검사
                { // 현재 파일 검사 시작
                    if (TryRead(current, out var found, out var hash)) // 현재 파일의 무결성과 기록 검사
                    { // 정상 파일 시작
                        entries = found; // 검증된 결말 기록 반영
                        expectedHash = hash; // 동시 변경 검사 기준 보관
                        return Snapshot(); // 외부 변경을 막는 기록 전달
                    } // 정상 파일 끝
                    blocked = true; // 손상 원본의 덮어쓰기 차단
                } // 현재 파일 검사 끝
                if (Directory.Exists(root)) // 복구 후보 폴더 검사
                { // 후보 검사 시작
                    var candidates = new List<string>(Directory.GetFiles(root, "ending-history-*.tmp")); // 중단된 임시 기록
                    candidates.AddRange(Directory.GetFiles(root, "ending-history-backup-*.json")); // 보존된 이전 기록
                    candidates.Sort((left, right) => // 저장 시도 시각의 최신 우선 정렬
                    { // 비교 시작
                        var order = CandidateTicks(right).CompareTo(CandidateTicks(left)); // 실제 저장 시각 비교
                        return order != 0 ? order : string.CompareOrdinal(right, left); // 동일 시각의 고정 순서
                    }); // 비교 끝
                    foreach (var candidate in candidates) // 저장 후보 순회
                    { // 후보 검사 시작
                        if (TryRead(candidate, out var recovered, out _)) // 후보 무결성과 영수증 검사
                        { // 복구 가능 시작
                            entries = recovered; // 검증된 후보의 읽기 전용 표시
                            blocked = true; // 원본과 복구 파일의 자동 변경 차단
                            warning = "결말 기록의 복구 파일을 읽었습니다. 원본 확인 전에는 저장할 수 없습니다."; // 복구와 보호 상태 안내
                            return Snapshot(); // 검증된 복구 기록 전달
                        } // 복구 가능 끝
                    } // 후보 검사 끝
                    blocked = blocked || candidates.Count > 0; // 손상된 복구 후보 보존
                } // 후보 검사 끝
                if (blocked) // 손상된 현재 파일 또는 후보 검사
                { // 손상 안내 시작
                    warning = "결말 기록을 읽을 수 없습니다. 기존 파일을 보존합니다."; // 실제 손상과 보호 안내
                } // 손상 안내 끝
                return Snapshot(); // 새 기록 또는 복구 불가의 빈 목록
            } // 파일 검사 끝
            catch (Exception) // 파일 경로 또는 접근 오류
            { // 조회 실패 시작
                blocked = true; // 접근할 수 없는 파일 보호
                warning = "결말 기록을 읽을 수 없습니다. 기존 파일을 보존합니다."; // 실제 조회 실패 안내
                return Snapshot(); // 변경하지 않는 기존 목록 전달
            } // 조회 실패 끝
        } // 조회 끝
        public bool IsUnlocked(string endingId) // 현재 기록의 결말 해금 여부
        { // 검사 시작
            foreach (var entry in entries) // 검증된 결말 기록 순회
            { // 결말 검사 시작
                if (string.Equals(entry.EndingId, endingId, StringComparison.Ordinal)) // 실제 결말 식별자 검사
                { // 일치 시작
                    return true; // 확인된 해금 상태
                } // 일치 끝
            } // 결말 검사 끝
            return false; // 기록되지 않은 결말
        } // 검사 끝
        public bool TryGetRun(string runId, out EndingHistoryEntry entry) // 실제 회차의 확정 결말 영수증
        { // 조회 시작
            entry = null; // 기본 기록 없음
            foreach (var item in entries) // 검증된 영수증 순회
            { // 회차 검사 시작
                if (string.Equals(item.RunId, runId, StringComparison.Ordinal)) // 정확한 회차 식별자 검사
                { // 일치 시작
                    entry = item; // 불변 영수증 전달
                    return true; // 현재 회차 기록 확인
                } // 일치 끝
            } // 회차 검사 끝
            return false; // 기록되지 않은 회차
        } // 조회 끝
        public bool TryRecord(string runId, string transactionId, string endingId, out bool firstUnlock, out int shards, out string error) // 회차 결말과 최초 보상의 안전한 확정
        { // 기록 시작
            firstUnlock = false; // 기본 최초 해금 없음
            shards = 0; // 기본 파편 지급 없음
            error = string.Empty; // 기본 오류 안내 없음
            if (!loaded || blocked || !ValidId(runId) || !Guid.TryParseExact(transactionId, "N", out var transactionGuid) || !EndingRules.TryFirstUnlockShards(endingId, out var possibleShards) || possibleShards < 0) // 최초 조회와 회차 및 결말 검사
            { // 기록 거절 시작
                error = "결말 기록을 저장할 수 없습니다. 기존 파일과 결과를 확인해 주세요."; // 유효하지 않은 저장 안내
                return false; // 원본과 파편 보존
            } // 기록 거절 끝
            transactionId = transactionGuid.ToString("N"); // 동일한 거래 식별자의 정규 형식
            var replaced = false; // 현재 파일 교체 여부
            try // 독점 잠금과 안전한 기록 교체
            { // 저장 처리 시작
                Directory.CreateDirectory(root); // 결말 저장 폴더 준비
                using (var gate = new FileStream(Path.Combine(root, "ending-history.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 다른 실행과 저장 작업 직렬화
                { // 잠금 시작
                    var exists = File.Exists(current); // 현재 파일 존재 여부
                    if (exists != (expectedHash != null)) // 읽은 뒤 생성 또는 삭제 검사
                    { // 변경 오류 시작
                        blocked = true; // 다른 실행의 파일 보호
                        error = "결말 기록이 다른 실행에서 변경되었습니다."; // 실제 동시 변경 안내
                        return false; // 오래된 상태 덮어쓰기 차단
                    } // 변경 오류 끝
                    var records = new List<EndingHistoryEntry>(); // 현재 파일의 독립 기록
                    if (exists && (!TryRead(current, out records, out var actualHash) || actualHash != expectedHash)) // 현재 파일 무결성과 변경 검사
                    { // 변경 또는 손상 시작
                        blocked = true; // 변경되거나 손상된 원본 보호
                        error = "결말 기록이 변경되었거나 손상되었습니다."; // 현재 파일 오류 안내
                        return false; // 기존 파일 덮어쓰기 차단
                    } // 변경 또는 손상 끝
                    var alreadyUnlocked = false; // 동일 결말의 이전 해금 여부
                    foreach (var existing in records) // 검증된 회차 기록 순회
                    { // 기록 검사 시작
                        var sameRun = string.Equals(existing.RunId, runId, StringComparison.Ordinal); // 동일 회차 여부
                        var sameTransaction = string.Equals(existing.TransactionId, transactionId, StringComparison.OrdinalIgnoreCase); // 동일 거래 여부
                        if (sameRun || sameTransaction) // 기존 회차 또는 거래 검사
                        { // 충돌 검사 시작
                            if (!sameRun || !sameTransaction || !string.Equals(existing.EndingId, endingId, StringComparison.Ordinal)) // 동일 거래의 다른 결과 검사
                            { // 충돌 시작
                                error = "같은 회차 또는 거래의 결말 기록이 다릅니다."; // 결과 불일치 안내
                                return false; // 기존 거래와 결과 보존
                            } // 충돌 끝
                            return true; // 동일한 거래의 무해한 재시도
                        } // 충돌 검사 끝
                        alreadyUnlocked |= string.Equals(existing.EndingId, endingId, StringComparison.Ordinal); // 기존 결말 해금 확인
                    } // 기록 검사 끝
                    if (records.Count >= 4096) // 기록 형식의 최대 수 검사
                    { // 기록 제한 시작
                        error = "결말 기록의 저장 가능한 수를 초과했습니다."; // 기록 한도 안내
                        return false; // 유효하지 않은 임시 파일 생성 차단
                    } // 기록 제한 끝
                    var isFirst = !alreadyUnlocked; // 실제 최초 해금 여부
                    var granted = isFirst ? possibleShards : 0; // 최초에만 지급할 파편
                    records.Add(new EndingHistoryEntry(runId, transactionId, endingId, isFirst, granted)); // 확정 결과와 지급 영수증 추가
                    var items = new Entry[records.Count]; // 직렬화할 독립 영수증 배열
                    for (var index = 0; index < records.Count; index++) // 검증된 기록 순회
                    { // 직렬화 시작
                        var item = records[index]; // 현재 영구 기록
                        items[index] = new Entry { runId = item.RunId, transactionId = item.TransactionId, endingId = item.EndingId, firstUnlock = item.FirstUnlock, grantedShards = item.GrantedShards }; // 모든 영수증 값 보존
                    } // 직렬화 끝
                    var payload = JsonUtility.ToJson(new Payload { version = 1, entries = items }); // 버전과 실제 영수증 본문
                    var envelope = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(Encoding.UTF8.GetBytes(payload)) }); // 체크섬 포함 저장 외피
                    var attempt = DateTime.UtcNow.Ticks.ToString("D19") + "-" + Guid.NewGuid().ToString("N"); // 충돌 없는 저장 시도 식별자
                    var temporary = Path.Combine(root, "ending-history-" + attempt + ".tmp"); // 같은 폴더의 임시 저장 파일
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 임시 파일 독립 생성
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // UTF8 문자 기록
                    { // 임시 저장 시작
                        writer.Write(envelope); // 체크섬 포함 내용 기록
                        writer.Flush(); // 문자 버퍼 반영
                        stream.Flush(true); // 저장 장치 반영
                    } // 임시 저장 끝
                    if (!TryRead(temporary, out var staged, out var stagedHash) || staged.Count != records.Count || stagedHash != Hash(Encoding.UTF8.GetBytes(envelope))) // 임시 파일 재읽기와 동일성 검사
                    { // 임시 검증 실패 시작
                        error = "결말 기록의 임시 저장을 검증하지 못했습니다."; // 임시 기록 오류 안내
                        return false; // 현재 파일과 파편 보존
                    } // 임시 검증 실패 끝
                    if (exists) // 기존 정상 기록 검사
                    { // 백업과 교체 시작
                        var backup = Path.Combine(root, "ending-history-backup-" + attempt + ".json"); // 이전 정상 기록 보관 경로
                        File.Copy(current, backup, false); // 기존 파일의 별도 백업
                        File.Replace(temporary, current, null); // 현재 기록의 원자적 교체
                    } // 백업과 교체 끝
                    else // 최초 저장 분기
                    { // 첫 기록 저장 시작
                        File.Move(temporary, current); // 없는 현재 파일에 임시 기록 배치
                    } // 첫 기록 저장 끝
                    replaced = true; // 현재 파일 교체 완료 표시
                    if (!TryRead(current, out var written, out var writtenHash) || writtenHash != stagedHash) // 교체된 파일과 임시 파일 비교
                    { // 최종 검증 실패 시작
                        blocked = true; // 추가 쓰기 차단과 복구 파일 보존
                        error = "저장한 결말 기록을 검증하지 못했습니다."; // 실제 최종 검증 오류
                        return false; // 확인되지 않은 결과 보고 차단
                    } // 최종 검증 실패 끝
                    entries = written; // 검증된 전체 결말과 최초 보상 반영
                    expectedHash = writtenHash; // 다음 동시 변경 검사 기준
                    firstUnlock = isFirst; // 실제 최초 해금 결과 전달
                    shards = granted; // 실제 이번 파편 지급량 전달
                    return true; // 검증된 기록과 지급 완료
                } // 잠금 끝
            } // 저장 처리 끝
            catch (Exception) // 잠금과 파일 접근 또는 교체 실패
            { // 실패 처리 시작
                blocked = blocked || replaced; // 교체 후 오류의 추가 저장 차단
                error = "결말 기록 저장에 실패했습니다. 저장 공간과 접근 권한을 확인해 주세요."; // 실제 저장 실패 안내
                return false; // 기존 기록과 결과 보존
            } // 실패 처리 끝
        } // 기록 끝
        private IReadOnlyList<EndingHistoryEntry> Snapshot() // 외부 변경이 불가능한 독립 기록 목록
        { // 목록 복사 시작
            return new List<EndingHistoryEntry>(entries).AsReadOnly(); // 독립 목록의 읽기 전용 전달
        } // 목록 복사 끝
        private static long CandidateTicks(string path) // 복구 후보의 저장 시각 조회
        { // 조회 시작
            var name = Path.GetFileName(path); // 후보 파일 이름
            var prefix = name.StartsWith("ending-history-backup-", StringComparison.Ordinal) ? "ending-history-backup-" : "ending-history-"; // 백업 또는 임시 접두사
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length < prefix.Length + 19) // 저장 시각 자리 검사
            { // 시각 누락 시작
                return 0; // 알 수 없는 시각의 낮은 우선순위
            } // 시각 누락 끝
            return long.TryParse(name.Substring(prefix.Length, 19), out var ticks) ? ticks : 0; // 검증된 저장 시각
        } // 조회 끝
        private static bool ValidId(string value) // 안전한 회차 식별자 검사
        { // 검사 시작
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Trim() != value) // 빈 값과 길이 및 외부 공백 검사
            { // 형식 오류 시작
                return false; // 잘못된 식별자 차단
            } // 형식 오류 끝
            foreach (var character in value) // 식별자 문자 순회
            { // 문자 검사 시작
                if (char.IsControl(character)) // 제어 문자 검사
                { // 제어 문자 시작
                    return false; // 잘못된 식별자 차단
                } // 제어 문자 끝
            } // 문자 검사 끝
            return true; // 저장 가능한 회차 식별자
        } // 검사 끝
        private static bool TryRead(string path, out List<EndingHistoryEntry> records, out string hash) // 무결성과 영수증 순서 재검증
        { // 조회 시작
            records = null; // 기본 기록 없음
            hash = null; // 기본 파일 해시 없음
            try // 실제 파일과 직렬화 검사
            { // 파일 검사 시작
                var info = new FileInfo(path); // 현재 파일 크기
                if (!info.Exists || info.Length > 1024 * 1024) // 파일 존재와 최대 크기 검사
                { // 파일 오류 시작
                    return false; // 잘못된 파일 차단
                } // 파일 오류 끝
                var bytes = File.ReadAllBytes(path); // 저장 파일 전체 읽기
                var envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(bytes)); // 체크섬 외피 해석
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum) || Hash(Encoding.UTF8.GetBytes(envelope.payload)) != envelope.checksum) // 체크섬과 본문 검사
                { // 무결성 오류 시작
                    return false; // 손상된 파일 차단
                } // 무결성 오류 끝
                var payload = JsonUtility.FromJson<Payload>(envelope.payload); // 실제 결말 기록 해석
                if (payload == null || payload.version != 1 || payload.entries == null || payload.entries.Length > 4096) // 저장 버전과 기록 수 검사
                { // 형식 오류 시작
                    return false; // 잘못된 저장 형식 차단
                } // 형식 오류 끝
                var runs = new HashSet<string>(StringComparer.Ordinal); // 기존 회차 식별자 중복 검사
                var transactions = new HashSet<Guid>(); // 마왕 거래 식별자 중복 검사
                var unlocked = new HashSet<string>(StringComparer.Ordinal); // 앞선 결말 해금 기록
                var result = new List<EndingHistoryEntry>(); // 검증된 독립 결과
                foreach (var item in payload.entries) // 저장된 시간순 영수증 순회
                { // 영수증 검사 시작
                    if (item == null || !ValidId(item.runId) || !Guid.TryParseExact(item.transactionId, "N", out var transaction) || !EndingRules.TryFirstUnlockShards(item.endingId, out var amount) || amount < 0 || !runs.Add(item.runId) || !transactions.Add(transaction)) // 회차와 거래 및 등록 결말 검사
                    { // 영수증 오류 시작
                        return false; // 중복과 위조된 기록 차단
                    } // 영수증 오류 끝
                    var first = unlocked.Add(item.endingId); // 이전 기록과 비교한 실제 최초 여부
                    if (item.firstUnlock != first || item.grantedShards != (first ? amount : 0)) // 기록 순서와 최초 보상 일관성 검사
                    { // 보상 오류 시작
                        return false; // 중복 또는 위조된 파편 차단
                    } // 보상 오류 끝
                    result.Add(new EndingHistoryEntry(item.runId, transaction.ToString("N"), item.endingId, first, item.grantedShards)); // 검증된 불변 영수증 보관
                } // 영수증 검사 끝
                records = result; // 검증된 전체 기록 전달
                hash = Hash(bytes); // 동시 변경 검사 기준 계산
                return true; // 검증된 파일 읽기 성공
            } // 파일 검사 끝
            catch (Exception) // 파일 접근 또는 형식 오류
            { // 읽기 오류 시작
                return false; // 손상된 파일 차단
            } // 읽기 오류 끝
        } // 조회 끝
        private static string Hash(byte[] bytes) // 저장 파일의 SHA256 해시
        { // 계산 시작
            using (var algorithm = SHA256.Create()) // 해시 계산 객체 생성
            { // 계산 객체 시작
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty); // 대문자 체크섬 문자열
            } // 계산 객체 끝
        } // 계산 끝
    } // 클래스 끝
} // 이름 공간 끝
