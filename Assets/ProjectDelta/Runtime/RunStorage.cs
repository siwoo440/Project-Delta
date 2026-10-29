using System; // 저장 오류와 고유 식별자
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
            try // 파일과 형식 검사
            { // 검사 시작
                if (!File.Exists(current)) // 저장 파일 존재 검사
                { // 파일 없음 시작
                    return null; // 이어할 회차 없음
                } // 파일 없음 끝
                if (TryRead(current, out var data, out var hash)) // 파일 무결성과 데이터 검사
                { // 정상 파일 시작
                    expectedHash = hash; // 동시 변경 비교 기준
                    return data; // 검증된 회차 데이터
                } // 정상 파일 끝
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
                    if (exists != (expectedHash != null) || (exists && (!TryRead(current, out _, out var actualHash) || actualHash != expectedHash))) // 최초 읽기 이후 변경 검사
                    { // 충돌 조건 시작
                        blocked = true; // 충돌 파일 덮어쓰기 차단
                        error = "회차 저장이 변경되었거나 손상되었습니다."; // 충돌 안내
                        return false; // 기존 파일 보존
                    } // 충돌 조건 끝
                    var payload = JsonUtility.ToJson(data); // 회차 데이터 직렬화
                    var envelope = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(Encoding.UTF8.GetBytes(payload)) }); // 무결성 값 포함
                    var temporary = Path.Combine(root, "run-" + Guid.NewGuid().ToString("N") + ".tmp"); // 고유 임시 파일
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
                        var backup = Path.Combine(root, "run-backup-" + DateTime.UtcNow.Ticks + "-" + Guid.NewGuid().ToString("N") + ".json"); // 충돌 없는 백업 경로
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
        private static string Hash(byte[] bytes) // SHA256 계산
        { // 계산 시작
            using (var hash = SHA256.Create()) // 해시 계산 도구
            { // 도구 사용 시작
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty); // 대문자 해시 문자열
            } // 도구 사용 끝
        } // 계산 끝
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
