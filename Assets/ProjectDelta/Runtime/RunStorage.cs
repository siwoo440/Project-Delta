using System; // 저장 오류와 고유 식별자
using System.Collections.Generic; // 복구 후보 목록
using System.IO; // 안전한 파일 접근
using System.Security.Cryptography; // 파일 무결성 검사
using System.Text; // 저장 문자 인코딩
using ProjectDelta.Core; // 회차 저장 데이터
using UnityEngine; // Unity 직렬화 도구
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class RunStorage // 회차 전용 저장 파일
    { // 클래스 시작
        [Serializable] // 외피 직렬화 표시
        private sealed class Envelope // 회차 본문과 무결성 값
        { // 클래스 시작
            public string payload; // 회차 데이터 본문
            public string checksum; // 본문의 해시
        } // 클래스 끝
        private readonly string root; // 회차 저장 폴더
        private readonly string current; // 현재 회차 저장 파일
        private string expectedHash; // 읽은 파일의 해시
        private string recoveredHash; // 보호 중인 복구 파일 해시
        public string LegacyRunId => (expectedHash ?? recoveredHash)?.Substring(0, 32); // 이전 저장의 안정적인 식별자
        private bool loaded; // 최초 읽기 완료 상태
        private bool blocked; // 손상이나 충돌 보호 상태
        public static string TestDirectoryOverride // 독립 검증의 저장 경로
        { // 속성 시작
            get; // 시험 경로 조회
            set; // 시험 경로 설정
        } // 속성 끝
        public static string ResolveDirectory() // 실제 실행과 검증 경로 구분
        { // 경로 조회 시작
            return string.IsNullOrEmpty(TestDirectoryOverride) ? ObservationStorage.ResolveDirectory() : TestDirectoryOverride; // 관찰 파일과 별도 이름 사용
        } // 경로 조회 끝
        public void BlockWrites() // 복원 오류 후 원본 보호
        { // 보호 시작
            blocked = true; // 추가 파일 교체 차단
        } // 보호 끝
        public RunStorage(string directory) // 저장 경로 연결
        { // 연결 시작
            root = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); // 절대 폴더 경로
            current = Path.Combine(root, "run-v1.json"); // 기존 관찰 기록과 분리된 경로
        } // 연결 끝
        public RunSaveData Load(out string warning) // 저장 파일과 오류 조회
        { // 조회 시작
            warning = string.Empty; // 기본 안내 없음
            loaded = true; // 읽기 시도 기록
            blocked = false; // 이전 차단 초기화
            expectedHash = null; // 읽은 해시 초기화
            recoveredHash = null; // 이전 복구 해시 초기화
            try // 파일과 형식 검사
            { // 검사 시작
                if (File.Exists(current) && TryReadEnded(current, out _, out var endedHash)) // 정상 종료된 회차 표시 검사
                { // 종료 기록 시작
                    expectedHash = endedHash; // 새 회차 저장의 변경 검사 기준
                    return null; // 종료한 회차와 이전 백업 재개 차단
                } // 종료 기록 끝
                if (File.Exists(current) && TryRead(current, out var data, out var hash)) // 현재 파일 무결성과 데이터 검사
                { // 정상 파일 시작
                    expectedHash = hash; // 동시 변경 비교 기준
                    return data; // 검증된 회차 데이터
                } // 정상 파일 끝
                blocked = File.Exists(current); // 손상된 현재 파일 보호
                if (Directory.Exists(root)) // 복구 후보 폴더 검사
                { // 후보 검사 시작
                    var candidates = new List<string>(Directory.GetFiles(root, "run-*.tmp")); // 저장 중 임시 파일 목록
                    candidates.AddRange(Directory.GetFiles(root, "run-backup-*.json")); // 기존 회차 백업 목록
                    candidates.Sort(CompareCandidates); // 최신 저장 시도 우선 정렬
                    foreach (var path in candidates) // 복구 후보 순회
                    { // 후보 검사 시작
                        if (TryRead(path, out var recovered, out var candidateHash)) // 후보 무결성과 회차 상태 검사
                        { // 복구 가능 조건 시작
                            blocked = true; // 원본과 복구 후보 보호
                            recoveredHash = candidateHash; // 복구 회차의 안정 식별자 보관
                            warning = "회차 저장에 문제가 있어 복구 기록을 읽었습니다. 파일 확인 전에는 저장할 수 없습니다."; // 복구와 저장 차단 안내
                            return recovered; // 검증된 복구 회차
                        } // 복구 가능 조건 끝
                    } // 후보 검사 끝
                    blocked = blocked || candidates.Count > 0; // 손상된 후보 파일 보호
                } // 후보 검사 끝
                if (!blocked) // 현재 파일과 복구 후보가 없는 새 게임 검사
                { // 새 게임 조건 시작
                    return null; // 이어할 회차 없음
                } // 새 게임 조건 끝
            } // 검사 끝
            catch (Exception) // 저장 접근 오류
            { // 접근 오류 시작
            } // 접근 오류 끝
            blocked = true; // 원본 덮어쓰기 차단
            warning = "회차 저장을 읽을 수 없습니다. 기존 파일을 확인해 주세요."; // 손상 또는 접근 오류 안내
            return null; // 유효하지 않은 회차 거절
        } // 조회 끝
        public bool TrySave(RunSaveData data, out string error) // 회차 저장 요청
        { // 저장 시작
            error = string.Empty; // 기본 오류 없음
            if (!loaded || blocked || !RunRestoreRules.IsValid(data)) // 최초 읽기와 저장 데이터 검사
            { // 저장 불가 조건 시작
                error = "회차 저장 상태 또는 데이터가 올바르지 않습니다."; // 저장 불가 안내
                return false; // 유효하지 않은 저장 차단
            } // 저장 불가 조건 끝
            try // 독점 잠금과 안전한 파일 교체
            { // 저장 처리 시작
                Directory.CreateDirectory(root); // 저장 폴더 준비
                using (var gate = new FileStream(Path.Combine(root, "run.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 동시 저장 잠금
                { // 잠금 시작
                    var exists = File.Exists(current); // 현재 파일 존재 여부
                    if (exists != (expectedHash != null) || (exists && (!TryReadCurrentHash(current, out var actualHash) || actualHash != expectedHash))) // 최초 읽기 이후 변경 검사
                    { // 충돌 조건 시작
                        blocked = true; // 충돌 파일 덮어쓰기 차단
                        error = "회차 저장이 변경되었거나 손상되었습니다."; // 충돌 안내
                        return false; // 기존 파일 보존
                    } // 충돌 조건 끝
                    var payload = JsonUtility.ToJson(data); // 회차 데이터 직렬화
                    var envelope = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(Encoding.UTF8.GetBytes(payload)) }); // 무결성 값 포함
                    var transaction = DateTime.UtcNow.Ticks.ToString("D19"); // 같은 저장 시도의 정렬 시각
                    var temporary = Path.Combine(root, "run-" + transaction + "-" + Guid.NewGuid().ToString("N") + ".tmp"); // 고유 임시 파일
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 새 임시 파일 생성
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // UTF8 문자 기록
                    { // 임시 저장 시작
                        writer.Write(envelope); // 저장 본문 기록
                        writer.Flush(); // 문자 버퍼 반영
                        stream.Flush(true); // 저장 장치 반영
                    } // 임시 저장 끝
                    if (!TryRead(temporary, out _, out _)) // 기록한 임시 파일 검증
                    { // 검증 실패 시작
                        error = "회차 저장을 검증하지 못했습니다."; // 오류 안내
                        return false; // 기존 파일 유지
                    } // 검증 실패 끝
                    if (exists) // 기존 파일 존재 검사
                    { // 교체 시작
                        var backup = Path.Combine(root, "run-backup-" + transaction + "-" + Guid.NewGuid().ToString("N") + ".json"); // 같은 저장 시도의 백업 경로
                        File.Copy(current, backup, false); // 기존 저장 별도 보존
                        File.Replace(temporary, current, null); // 기존 파일의 원자적 교체
                    } // 교체 끝
                    else // 최초 저장 분기
                    { // 최초 저장 시작
                        File.Move(temporary, current); // 새 파일의 원자적 배치
                    } // 최초 저장 끝
                    if (!TryRead(current, out _, out expectedHash)) // 저장한 파일 재검증
                    { // 재검증 실패 시작
                        blocked = true; // 추가 저장 차단
                        error = "저장한 회차를 확인하지 못했습니다."; // 오류 안내
                        return false; // 검증 실패 보고
                    } // 재검증 실패 끝
                    return true; // 검증된 저장 완료
                } // 잠금 끝
            } // 저장 처리 끝
            catch (Exception) // 저장 접근과 교체 오류
            { // 오류 처리 시작
                error = "회차 저장에 실패했습니다. 저장 공간과 권한을 확인해 주세요."; // 저장 실패 안내
                return false; // 실패 상태 전달
            } // 오류 처리 끝
        } // 저장 끝
        public bool TryMarkEnded(string runId, out string error) // 영구 기록 후 회차 종료 표시
        { // 종료 시작
            error = string.Empty; // 기본 오류 없음
            if (!loaded || blocked || string.IsNullOrWhiteSpace(runId) || runId.Length > 128) // 저장 상태와 회차 식별자 검사
            { // 종료 거절 시작
                error = "회차 종료 상태를 기록할 수 없습니다."; // 종료 불가 안내
                return false; // 기존 회차 보존
            } // 종료 거절 끝
            try // 회차 잠금과 안전한 종료 표시
            { // 종료 처리 시작
                Directory.CreateDirectory(root); // 회차 저장 폴더 준비
                using (var gate = new FileStream(Path.Combine(root, "run.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 동시 저장 잠금
                { // 잠금 시작
                    var exists = File.Exists(current); // 현재 파일 존재 여부
                    if (exists != (expectedHash != null) || (exists && (!TryReadCurrentHash(current, out var actualHash) || actualHash != expectedHash))) // 다른 실행의 변경 검사
                    { // 충돌 처리 시작
                        blocked = true; // 다른 실행의 저장 보호
                        error = "회차 저장이 변경되었거나 손상되었습니다."; // 충돌 안내
                        return false; // 기존 회차 보존
                    } // 충돌 처리 끝
                    if (exists && TryReadEnded(current, out var previousId, out _) && previousId == runId) // 같은 회차의 이미 완료된 종료 검사
                    { // 중복 종료 시작
                        return true; // 같은 종료의 중복 처리 방지
                    } // 중복 종료 끝
                    if (exists && TryRead(current, out var existingRun, out var existingHash)) // 다른 회차의 기존 저장 검사
                    { // 회차 식별 검사 시작
                        var existingId = existingRun.version < 7 ? existingHash.Substring(0, 32) : existingRun.runId; // 이전 저장과 새 저장의 식별자
                        if (existingId != runId) // 다른 회차 저장 검사
                        { // 다른 회차 보존 시작
                            return true; // 다른 회차 파일과 백업 유지
                        } // 다른 회차 보존 끝
                    } // 회차 식별 검사 끝
                    var payload = "ended:" + runId; // 종료한 회차 식별 내용
                    var envelope = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(Encoding.UTF8.GetBytes(payload)) }); // 종료 내용과 무결성 값
                    var temporary = Path.Combine(root, "end-" + Guid.NewGuid().ToString("N") + ".tmp"); // 다른 복구 후보와 구분한 임시 파일
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 종료 임시 파일 생성
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // 종료 기록 문자 인코딩
                    { // 종료 임시 저장 시작
                        writer.Write(envelope); // 종료 기록 쓰기
                        writer.Flush(); // 문자 버퍼 반영
                        stream.Flush(true); // 저장 장치 반영
                    } // 종료 임시 저장 끝
                    if (!TryReadEnded(temporary, out var writtenId, out _) || writtenId != runId) // 임시 종료 기록 검증
                    { // 종료 기록 오류 시작
                        error = "회차 종료 기록을 검증하지 못했습니다."; // 검증 실패 안내
                        return false; // 기존 회차 보존
                    } // 종료 기록 오류 끝
                    var candidates = new List<string>(Directory.GetFiles(root, "run-*.tmp")); // 이전 회차의 임시 복구 후보
                    candidates.AddRange(Directory.GetFiles(root, "run-backup-*.json")); // 이전 회차의 백업 복구 후보
                    foreach (var candidate in candidates) // 기존 복구 후보 순회
                    { // 안전한 보관 시작
                        var retired = Path.Combine(root, "retired-" + Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(candidate)); // 복구 대상에서 분리할 보관 경로
                        File.Move(candidate, retired); // 원본 데이터를 삭제하지 않고 보관
                    } // 안전한 보관 끝
                    if (exists) // 기존 회차 존재 검사
                    { // 기존 회차 보관 시작
                        File.Copy(current, Path.Combine(root, "retired-" + Guid.NewGuid().ToString("N") + "-current.json"), false); // 종료 전 원본 보관
                        File.Replace(temporary, current, null); // 종료 표시의 원자적 교체
                    } // 기존 회차 보관 끝
                    else // 기존 회차 파일 없음 분기
                    { // 최초 종료 표시 시작
                        File.Move(temporary, current); // 종료 표시의 원자적 생성
                    } // 최초 종료 표시 끝
                    if (!TryReadEnded(current, out var confirmedId, out expectedHash) || confirmedId != runId) // 종료 표시의 재검증
                    { // 종료 검증 실패 시작
                        blocked = true; // 추가 저장 차단
                        error = "회차 종료 상태를 확인하지 못했습니다."; // 종료 검증 오류 안내
                        return false; // 검증되지 않은 종료 보고
                    } // 종료 검증 실패 끝
                    return true; // 검증된 종료 완료
                } // 잠금 끝
            } // 종료 처리 끝
            catch (Exception) // 저장 접근 또는 교체 오류
            { // 오류 처리 시작
                error = "회차 종료에 실패했습니다. 저장 파일을 확인해 주세요."; // 종료 실패 안내
                return false; // 기존 자료 보존과 실패 전달
            } // 오류 처리 끝
        } // 종료 끝
        private static bool TryReadCurrentHash(string path, out string hash) // 정상 회차 또는 종료 표시의 파일 해시 조회
        { // 조회 시작
            if (TryRead(path, out _, out hash)) // 정상 회차 검사
            { // 회차 조건 시작
                return true; // 정상 회차 해시 전달
            } // 회차 조건 끝
            return TryReadEnded(path, out _, out hash); // 검증된 종료 표시 해시 전달
        } // 조회 끝
        private static bool TryReadEnded(string path, out string runId, out string hash) // 종료 표시와 무결성 검사
        { // 검사 시작
            runId = null; // 기본 회차 식별자 없음
            hash = null; // 기본 파일 해시 없음
            try // 종료 표시 읽기
            { // 읽기 시작
                var info = new FileInfo(path); // 종료 파일 정보
                if (!info.Exists || info.Length > 1024 * 1024) // 파일 존재와 크기 검사
                { // 파일 오류 시작
                    return false; // 잘못된 파일 차단
                } // 파일 오류 끝
                var bytes = File.ReadAllBytes(path); // 전체 종료 파일 읽기
                var envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(bytes)); // 종료 외피 해석
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || !envelope.payload.StartsWith("ended:", StringComparison.Ordinal) || envelope.checksum != Hash(Encoding.UTF8.GetBytes(envelope.payload))) // 종료 형식과 무결성 검사
                { // 형식 오류 시작
                    return false; // 잘못된 종료 표시 차단
                } // 형식 오류 끝
                var id = envelope.payload.Substring(6); // 종료한 회차 식별자
                if (string.IsNullOrWhiteSpace(id) || id.Length > 128) // 식별자 유효성 검사
                { // 식별자 오류 시작
                    return false; // 잘못된 종료 표시 차단
                } // 식별자 오류 끝
                runId = id; // 검증된 회차 식별자 전달
                hash = Hash(bytes); // 전체 파일의 변경 검사 해시
                return true; // 종료 표시 확인
            } // 읽기 끝
            catch (Exception) // 파일과 직렬화 오류
            { // 오류 처리 시작
                return false; // 손상된 종료 표시 차단
            } // 오류 처리 끝
        } // 검사 끝
        private static int CompareCandidates(string left, string right) // 복구 후보의 최신순 비교
        { // 비교 시작
            var order = CandidateTicks(right).CompareTo(CandidateTicks(left)); // 저장 시도 시각 비교
            if (order != 0) // 다른 저장 시각 검사
            { // 시각 조건 시작
                return order; // 최신 시도 우선
            } // 시각 조건 끝
            var leftPending = left.EndsWith(".tmp", StringComparison.Ordinal); // 왼쪽 임시 파일 여부
            var rightPending = right.EndsWith(".tmp", StringComparison.Ordinal); // 오른쪽 임시 파일 여부
            return leftPending != rightPending ? (leftPending ? -1 : 1) : string.CompareOrdinal(right, left); // 같은 시도의 임시 기록 우선
        } // 비교 끝
        private static long CandidateTicks(string path) // 복구 파일의 정렬 시각
        { // 시각 조회 시작
            var name = Path.GetFileName(path); // 복구 파일 이름
            var prefix = name.StartsWith("run-backup-", StringComparison.Ordinal) ? "run-backup-" : "run-"; // 백업 또는 임시 파일 접두사
            var separator = name.IndexOf('-', prefix.Length); // 시각 다음 구분자 위치
            if (name.StartsWith(prefix, StringComparison.Ordinal) && separator > prefix.Length && separator - prefix.Length <= 19 && long.TryParse(name.Substring(prefix.Length, separator - prefix.Length), out var ticks)) // 기존과 새 형식의 저장 시각 검사
            { // 저장 시각 조건 시작
                return ticks; // 기록된 저장 시각
            } // 저장 시각 조건 끝
            return File.GetLastWriteTimeUtc(path).Ticks; // 이전 임시 파일의 파일 수정 시각
        } // 시각 조회 끝
        private static string Hash(byte[] bytes) // SHA256 계산
        { // 계산 시작
            using (var hash = SHA256.Create()) // 해시 계산 도구
            { // 도구 사용 시작
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty); // 대문자 해시 문자열
            } // 도구 사용 끝
        } // 계산 끝
        public static void NormalizeEquipment(RunSaveData data) // 역직렬화된 빈 장비 객체 정리
        { // 정리 시작
            if (data == null) // 회차 데이터 검사
            { // 누락 시작
                return; // 정리할 데이터 없음
            } // 누락 끝
            data.weaponVariant = NormalizeVariant(data.weaponVariant); // 빈 무기 특성 정리
            data.armorVariant = NormalizeVariant(data.armorVariant); // 빈 방어구 특성 정리
            data.accessoryVariant = NormalizeVariant(data.accessoryVariant); // 빈 장신구 특성 정리
            data.bagVariant = NormalizeVariant(data.bagVariant); // 빈 가방 특성 정리
            if (data.pendingReward != null) // 보상 후보 검사
            { // 후보 장비 정리 시작
                data.pendingReward.equipment = NormalizeVariant(data.pendingReward.equipment); // 빈 보상 장비 객체 정리
            } // 후보 장비 정리 끝
            if (data.shopStocks != null) // 저장된 층별 상점 검사
            { // 상점 순회 시작
                foreach (var shop in data.shopStocks) // 개별 상점 순회
                { // 상점 정리 시작
                    if (shop == null || shop.offers == null) // 누락 상점과 상품 목록 검사
                    { // 누락 분기 시작
                        continue; // 손상 여부는 저장 검증에서 처리
                    } // 누락 분기 끝
                    foreach (var offer in shop.offers) // 각 상품 순회
                    { // 상품 정리 시작
                        if (offer != null) // 실제 상품 검사
                        { // 장비 정리 시작
                            offer.equipment = NormalizeVariant(offer.equipment); // 빈 상품 장비 정보 정리
                        } // 장비 정리 끝
                    } // 상품 정리 끝
                } // 상점 정리 끝
            } // 상점 순회 끝
            if (data.slots != null) // 저장 슬롯 목록 검사
            { // 슬롯 순회 시작
                foreach (var slot in data.slots) // 저장 슬롯 순회
                { // 슬롯 정리 시작
                    if (slot != null) // 유효 슬롯 검사
                    { // 개별 장비 정리 시작
                        slot.equipment = NormalizeVariant(slot.equipment); // 빈 보유 장비 특성 정리
                    } // 개별 장비 정리 끝
                } // 슬롯 정리 끝
            } // 슬롯 순회 끝
        } // 정리 끝
        private static EquipmentVariantData NormalizeVariant(EquipmentVariantData item) // Unity의 빈 중첩 객체 정리
        { // 정리 시작
            if (item == null) // 원래 빈 장비 검사
            { // 빈 장비 시작
                return null; // 빈 값 유지
            } // 빈 장비 끝
            var plan = item.purification; // 직렬화된 정화 계획
            var emptyPlan = plan == null || string.IsNullOrEmpty(plan.sourceDefinitionId) && string.IsNullOrEmpty(plan.kind) && string.IsNullOrEmpty(plan.resultId) && plan.resultQuantity == 0 && (plan.resultEffects == null || plan.resultEffects.Length == 0); // 모든 필드가 빈 정화 계획
            if (emptyPlan) // Unity가 만든 빈 정화 객체 검사
            { // 빈 계획 시작
                item.purification = null; // 빈 계획을 원래 값으로 복원
            } // 빈 계획 끝
            if (string.IsNullOrEmpty(item.instanceId) && string.IsNullOrEmpty(item.definitionId) && string.IsNullOrEmpty(item.grade) && (item.options == null || item.options.Length == 0) && !item.cursed && (item.curseEffects == null || item.curseEffects.Length == 0) && emptyPlan) // 모든 필드가 빈 개별 장비 검사
            { // 빈 장비 시작
                return null; // Unity의 빈 중첩 객체 제거
            } // 빈 장비 끝
            return item; // 실제 장비와 손상 여부 검사 유지
        } // 정리 끝
        private static bool TryRead(string path, out RunSaveData data, out string hash) // 파일 무결성과 버전 검사
        { // 검사 시작
            data = null; // 기본 데이터 없음
            hash = null; // 기본 해시 없음
            try // 파일 읽기
            { // 읽기 시작
                var info = new FileInfo(path); // 파일 정보 조회
                if (!info.Exists || info.Length > 1024 * 1024) // 파일 존재와 크기 검사
                { // 크기 오류 시작
                    return false; // 범위 밖 파일 거절
                } // 크기 오류 끝
                var bytes = File.ReadAllBytes(path); // 파일 전체 읽기
                var envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(bytes)); // 외피 해석
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum) || envelope.checksum != Hash(Encoding.UTF8.GetBytes(envelope.payload))) // 무결성 검사
                { // 손상 조건 시작
                    return false; // 손상 파일 거절
                } // 손상 조건 끝
                var candidate = JsonUtility.FromJson<RunSaveData>(envelope.payload); // 회차 데이터 해석
                if (candidate != null && !candidate.hasPendingReward) // 직렬화된 빈 보상 객체 검사
                { // 빈 후보 정리 시작
                    candidate.pendingReward = null; // 완료 또는 이전 저장의 빈 후보 정리
                } // 빈 후보 정리 끝
                if (candidate != null && candidate.version < 10 && candidate.hasPendingReward && candidate.pendingReward != null && candidate.pendingReward.potionCount > 0) // 이전 버전의 소형 회복약 후보 검사
                { // 이전 보상 복원 시작
                    candidate.pendingReward.itemId = "trial-small-potion"; // 기존 보상의 실제 소비품 종류 보존
                } // 이전 보상 복원 끝
                NormalizeEquipment(candidate); // Unity의 빈 장비 객체 정리
                if (!RunRestoreRules.IsValid(candidate)) // 버전과 던전 상태 검사
                { // 형식 오류 시작
                    return false; // 지원하지 않는 회차 거절
                } // 형식 오류 끝
                data = candidate; // 검증된 데이터 전달
                hash = Hash(bytes); // 동시 변경 검사 해시
                return true; // 읽기 성공
            } // 읽기 끝
            catch (Exception) // 파일 접근 또는 형식 오류
            { // 오류 처리 시작
                return false; // 원본 보존과 읽기 거절
            } // 오류 처리 끝
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
