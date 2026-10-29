using System; // 저장 오류와 고유 식별자
using System.Collections.Generic; // 백업 목록 정렬
using System.IO; // 안전한 파일 쓰기
using System.Security.Cryptography; // 저장 내용 무결성 검사
using System.Text; // 저장 파일 문자 인코딩
using ProjectDelta.Core; // 관계 기록
using UnityEngine; // Unity 저장 경로와 직렬화
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class NpcRelationshipStorage // 2D 전용 관계 기록 저장
    { // 클래스 시작
        [Serializable] // Unity 직렬화 형식
        private sealed class Payload // 검증할 관계 기록 본문
        { // 클래스 시작
            public int version; // 저장 형식 버전
            public NpcRelationshipSaveData data; // 관계와 지급 기록
        } // 클래스 끝
        [Serializable] // Unity 직렬화 형식
        private sealed class Envelope // 관계 기록과 무결성 값
        { // 클래스 시작
            public string payload; // 직렬화한 기록 본문
            public string checksum; // 본문의 SHA256 값
        } // 클래스 끝
        private readonly string root; // 관계 저장 폴더
        private readonly string current; // 현재 관계 파일
        private string expectedHash; // 읽은 파일 해시
        private bool blocked; // 저장 차단 상태
        public bool IsBlocked => blocked; // 복구 필요 상태
        public NpcRelationshipStorage(string directory) // 전용 저장 폴더 연결
        { // 연결 시작
            root = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); // 절대 저장 경로 보관
            current = Path.Combine(root, "npc-relationship-v1.json"); // 현재 관계 기록 경로
        } // 연결 끝
        public NpcRelationshipState Load(out string warning) // 현재 기록과 보호된 복구 조회
        { // 조회 시작
            warning = string.Empty; // 기본 안내 없음
            blocked = false; // 새 조회의 저장 차단 초기화
            expectedHash = null; // 현재 파일 해시 초기화
            try // 현재 파일과 복구 후보 검사
            { // 파일 검사 시작
                if (File.Exists(current)) // 현재 저장 파일 존재 검사
                { // 현재 파일 검사 시작
                    if (TryRead(current, out var loaded, out var hash)) // 버전과 체크섬 검증
                    { // 정상 파일 조건 시작
                        expectedHash = hash; // 동시 변경 검사 기준 보관
                        return loaded; // 정상 관계 기록 전달
                    } // 정상 파일 조건 끝
                    blocked = true; // 손상된 현재 파일의 덮어쓰기 차단
                } // 현재 파일 검사 끝
                if (Directory.Exists(root)) // 복구 후보 폴더 검사
                { // 복구 후보 검사 시작
                    var candidates = new List<string>(Directory.GetFiles(root, "npc-relationship-*.tmp")); // 중단된 임시 저장 목록
                    var backups = new List<string>(Directory.GetFiles(root, "npc-relationship-backup-*.json")); // 자동 백업 목록
                    candidates.AddRange(backups); // 임시 파일과 백업을 함께 검사
                    candidates.Sort((left, right) => // 전체 후보의 최신 기록 우선 정렬
                    { // 후보 비교 시작
                        var order = CandidateTicks(right).CompareTo(CandidateTicks(left)); // 파일 이름의 생성 시각 비교
                        if (order != 0) // 다른 저장 시도의 시각 검사
                        { // 시각 순서 시작
                            return order; // 최신 저장 시도 우선
                        } // 시각 순서 끝
                        var leftPending = left.EndsWith(".tmp", StringComparison.Ordinal); // 왼쪽 임시 기록 여부
                        var rightPending = right.EndsWith(".tmp", StringComparison.Ordinal); // 오른쪽 임시 기록 여부
                        return leftPending != rightPending ? (leftPending ? -1 : 1) : string.CompareOrdinal(right, left); // 같은 시도의 최신 임시 기록 우선
                    }); // 후보 비교 끝
                    foreach (var path in candidates) // 검증 가능한 복구 후보 순회
                    { // 복구 후보 시작
                        if (TryRead(path, out var recovered, out _)) // 후보 버전과 체크섬 검사
                        { // 유효한 후보 조건 시작
                            blocked = true; // 원본과 복구 후보의 자동 덮어쓰기 차단
                            warning = "관계 저장에 문제가 있어 복구 파일을 읽었습니다. 원본 확인 전에는 새 기록을 저장할 수 없습니다."; // 복구와 보호 상태 안내
                            return recovered; // 안전하게 읽은 관계 기록
                        } // 유효한 후보 조건 끝
                    } // 복구 후보 끝
                    if (candidates.Count > 0) // 손상된 복구 파일 존재 검사
                    { // 복구 불가 조건 시작
                        blocked = true; // 기존 후보 파일 보존
                    } // 복구 불가 조건 끝
                } // 복구 후보 검사 끝
                if (blocked) // 손상된 파일 존재 검사
                { // 손상 안내 시작
                    warning = "관계 저장을 읽을 수 없습니다. 기존 파일을 보존하며 새 기록 저장을 중단합니다."; // 원본 보존 안내
                } // 손상 안내 끝
                return NpcRelationshipState.Empty; // 새 기록 또는 복구 불가 시 빈 표시
            } // 파일 검사 끝
            catch (Exception) // 파일 접근 또는 읽기 실패
            { // 읽기 실패 시작
                blocked = true; // 접근 불가 파일의 덮어쓰기 차단
                warning = "관계 저장을 읽을 수 없습니다. 기존 파일을 보존하며 새 기록 저장을 중단합니다."; // 사용자에게 읽기 오류 안내
                return NpcRelationshipState.Empty; // 잘못된 파일을 수정하지 않는 빈 표시
            } // 읽기 실패 끝
        } // 조회 끝
        public bool TrySave(NpcRelationshipState journal, out string error) // 검증된 임시 파일의 안전한 교체
        { // 저장 시작
            error = string.Empty; // 기본 오류 없음
            if (blocked || journal == null) // 보호 상태와 관계 기록 검사
            { // 저장 거절 조건 시작
                error = "관계 기록을 저장할 수 없습니다. 기존 저장 파일을 확인해 주세요."; // 저장 차단 안내
                return false; // 손상 원본과 잘못된 기록 보존
            } // 저장 거절 조건 끝
            if (!NpcRelationshipState.TryRestore(journal.Snapshot(), out _)) // 저장 상태 유효성 검사
            { // 잘못된 상태 조건 시작
                error = "관계 기록이 올바르지 않습니다."; // 유효성 오류 안내
                return false; // 잘못된 상태 거절
            } // 잘못된 상태 조건 끝
            try // 동일 폴더의 임시 기록과 원자적 교체
            { // 저장 처리 시작
                Directory.CreateDirectory(root); // 전용 저장 폴더 준비
                var lockPath = Path.Combine(root, "npc-relationship.lock"); // 동시 저장 보호 파일
                using (var gate = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 다른 실행과 저장 작업 직렬화
                { // 잠금 시작
                    var exists = File.Exists(current); // 현재 기록 존재 검사
                    if (exists != (expectedHash != null)) // 읽은 뒤 다른 실행의 생성 또는 삭제 검사
                    { // 동시 변경 조건 시작
                        blocked = true; // 예상하지 못한 파일 덮어쓰기 차단
                        error = "관계 저장이 다른 실행에서 변경되었습니다. 기존 파일을 확인해 주세요."; // 충돌 안내
                        return false; // 다른 실행의 데이터 보존
                    } // 동시 변경 조건 끝
                    if (exists && (!TryRead(current, out _, out var actualHash) || actualHash != expectedHash)) // 읽은 현재 파일의 변경과 손상 검사
                    { // 파일 변경 조건 시작
                        blocked = true; // 변경된 현재 파일 덮어쓰기 차단
                        error = "관계 저장이 변경되었거나 손상되었습니다. 기존 파일을 확인해 주세요."; // 충돌 또는 손상 안내
                        return false; // 다른 실행과 손상 파일 보존
                    } // 파일 변경 조건 끝
                    var payload = JsonUtility.ToJson(new Payload // 저장할 관계 기록 본문
                    { // 본문 구성 시작
                        version = 1, // 현재 저장 형식 버전
                        data = journal.Snapshot() // 관계와 지급 기록
                    }); // 본문 구성 끝
                    var envelope = JsonUtility.ToJson(new Envelope // 체크섬을 포함한 저장 파일
                    { // 저장 구성 시작
                        payload = payload, // 검증할 본문
                        checksum = Hash(Encoding.UTF8.GetBytes(payload)) // 본문의 무결성 값
                    }); // 저장 구성 끝
                    var transaction = DateTime.UtcNow.Ticks.ToString("D19"); // 같은 저장 시도의 복구 순서
                    var temp = Path.Combine(root, "npc-relationship-" + transaction + "-" + Guid.NewGuid().ToString("N") + ".tmp"); // 충돌 없는 임시 저장 파일
                    using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 기존 임시 파일 덮어쓰기 방지
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // UTF8 저장 문자 쓰기
                    { // 임시 파일 작성 시작
                        writer.Write(envelope); // 직렬화한 저장 내용 기록
                        writer.Flush(); // 문자 버퍼 반영
                        stream.Flush(true); // 기록을 저장 장치에 반영
                    } // 임시 파일 작성 끝
                    if (!TryRead(temp, out _, out _)) // 임시 파일 재읽기와 무결성 검사
                    { // 임시 파일 손상 조건 시작
                        error = "관계 저장을 검증하지 못했습니다. 기존 파일은 유지됩니다."; // 검증 실패 안내
                        return false; // 기존 현재 파일 보존
                    } // 임시 파일 손상 조건 끝
                    if (exists) // 기존 정상 파일 존재 검사
                    { // 백업과 교체 시작
                        var backup = Path.Combine(root, "npc-relationship-backup-" + transaction + "-" + Guid.NewGuid().ToString("N") + ".json"); // 같은 시도의 고유 백업 경로
                        File.Copy(current, backup, false); // 이전 정상 기록의 별도 백업
                        File.Replace(temp, current, null); // 현재 파일의 원자적 교체
                    } // 백업과 교체 끝
                    else // 최초 저장 분기
                    { // 최초 저장 시작
                        File.Move(temp, current); // 없는 현재 파일에만 임시 기록 이동
                    } // 최초 저장 끝
                    if (!TryRead(current, out _, out expectedHash)) // 교체한 파일의 무결성 재확인
                    { // 교체 검증 실패 조건 시작
                        blocked = true; // 추가 저장 차단
                        error = "저장한 관계 기록을 확인하지 못했습니다. 기존 백업을 확인해 주세요."; // 검증 실패 안내
                        return false; // 잘못된 저장 성공 차단
                    } // 교체 검증 실패 조건 끝
                    TrimBackups(); // 오래된 정상 백업 정리
                    return true; // 검증된 관계 기록 저장 성공
                } // 잠금 끝
            } // 저장 처리 끝
            catch (Exception) // 파일 접근과 교체 실패
            { // 저장 실패 시작
                error = "관계 기록 저장에 실패했습니다. 저장 공간과 접근 권한을 확인해 주세요."; // 사용자에게 저장 실패 안내
                return false; // 기존 현재 파일 보존
            } // 저장 실패 끝
        } // 저장 끝
        private static long CandidateTicks(string path) // 복구 파일 이름의 생성 시각 조회
        { // 시각 조회 시작
            var name = Path.GetFileName(path); // 복구 파일 이름 조회
            var prefix = name.StartsWith("npc-relationship-backup-", StringComparison.Ordinal) ? "npc-relationship-backup-" : "npc-relationship-"; // 백업 또는 임시 파일 접두사
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length < prefix.Length + 19) // 생성 시각의 위치 검사
            { // 이름 오류 조건 시작
                return 0; // 알 수 없는 파일을 뒤에서 검사
            } // 이름 오류 조건 끝
            return long.TryParse(name.Substring(prefix.Length, 19), out var ticks) ? ticks : 0; // 유효한 시간 또는 기본 순서
        } // 시각 조회 끝
        private static string Hash(byte[] bytes) // 저장 내용의 SHA256 계산
        { // 계산 시작
            using (var hash = SHA256.Create()) // 해시 계산 객체 생성
            { // 계산 객체 시작
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty); // 대문자 체크섬 문자열
            } // 계산 객체 끝
        } // 계산 끝
        private static bool TryRead(string path, out NpcRelationshipState journal, out string hash) // 파일 버전과 체크섬 및 식별자 검증
        { // 검증 시작
            journal = null; // 관계 기록 초기화
            hash = null; // 전체 파일 해시 초기화
            try // 저장 파일 읽기와 구조 검사
            { // 파일 검사 시작
                var info = new FileInfo(path); // 저장 파일 크기 조회
                if (!info.Exists || info.Length > 1024 * 1024) // 유효한 파일과 최대 크기 검사
                { // 크기 오류 조건 시작
                    return false; // 예상 범위를 벗어난 파일 거절
                } // 크기 오류 조건 끝
                var bytes = File.ReadAllBytes(path); // 현재 파일 전체 읽기
                var envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(bytes)); // 저장 외피 해석
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum) || Hash(Encoding.UTF8.GetBytes(envelope.payload)) != envelope.checksum) // 본문과 체크섬 검사
                { // 무결성 오류 조건 시작
                    return false; // 손상된 파일 거절
                } // 무결성 오류 조건 끝
                var payload = JsonUtility.FromJson<Payload>(envelope.payload); // 관계 기록 본문 해석
                if (payload == null || payload.version != 1 || !NpcRelationshipState.TryRestore(payload.data, out var result)) // 형식과 관계 기록 검사
                { // 형식 오류 조건 시작
                    return false; // 잘못된 저장 기록 거절
                } // 형식 오류 조건 끝
                journal = result; // 검증된 관계 기록 전달
                hash = Hash(bytes); // 동시 변경 검사용 전체 해시
                return true; // 검증된 파일 읽기 성공
            } // 파일 검사 끝
            catch (Exception) // 읽기 또는 해석 실패
            { // 실패 처리 시작
                return false; // 손상되거나 접근할 수 없는 파일 거절
            } // 실패 처리 끝
        } // 검증 끝
        private void TrimBackups() // 최근 정상 백업 세 개 보존
        { // 정리 시작
            try // 저장 성공 후 소유한 정상 백업 정리
            { // 백업 검사 시작
                var paths = new List<string>(Directory.GetFiles(root, "npc-relationship-backup-*.json")); // 자동 백업 목록
                paths.Sort(StringComparer.Ordinal); // 생성 시간순 이름 정렬
                var valid = new List<string>(); // 검증된 정상 백업 목록
                foreach (var path in paths) // 백업 파일 순회
                { // 백업 검증 시작
                    if (TryRead(path, out _, out _)) // 무결성이 유효한 백업 검사
                    { // 정상 백업 조건 시작
                        valid.Add(path); // 삭제 가능한 정상 백업만 보관
                    } // 정상 백업 조건 끝
                } // 백업 검증 끝
                while (valid.Count > 3) // 최근 백업 세 개 초과 검사
                { // 오래된 백업 정리 시작
                    File.Delete(valid[0]); // 가장 오래된 정상 백업 정리
                    valid.RemoveAt(0); // 정리한 백업 목록 제거
                } // 오래된 백업 정리 끝
            } // 백업 검사 끝
            catch (Exception) // 백업 정리 실패
            { // 정리 실패 시작
                return; // 정상 현재 저장을 유지하며 추가 백업 보존
            } // 정리 실패 끝
        } // 정리 끝
    } // 클래스 끝
} // 이름 공간 끝
