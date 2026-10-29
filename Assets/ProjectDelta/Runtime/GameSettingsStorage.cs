using System; // 오류와 식별자
using System.Collections.Generic; // 복구 후보 목록
using System.IO; // 설정 파일 저장
using System.Security.Cryptography; // 무결성 확인
using System.Text; // 파일 인코딩
using ProjectDelta.Core; // 설정 값 규칙
using UnityEngine; // Unity 직렬화와 저장 경로
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class GameSettingsStorage // 독립 설정 저장소
    { // 저장소 시작
        [Serializable] // Unity 본문 직렬화
        private sealed class Payload // 설정 저장 본문
        { // 본문 시작
            public int version; // 저장 버전
            public int masterVolume; // 전체 음량
            public int encounterFlash; // 조우 섬광
        } // 본문 끝
        [Serializable] // Unity 외피 직렬화
        private sealed class Envelope // 무결성 포함 파일
        { // 외피 시작
            public string payload; // 설정 본문
            public string checksum; // 본문 무결성 값
        } // 외피 끝
        private readonly string root; // 설정 폴더
        private readonly string current; // 현재 설정 파일
        private string expectedHash; // 마지막 검증 파일 해시
        private bool loaded; // 최초 조회 완료
        private bool blocked; // 손상이나 충돌 보호
        public bool IsBlocked => blocked; // 저장 보호 상태
        public static string TestDirectoryOverride { get; set; } // 독립 시험 경로
        public static string ResolveDirectory() // 설정 저장 경로
        { // 경로 선택 시작
            if (!string.IsNullOrEmpty(TestDirectoryOverride)) // 시험 경로 검사
            { // 시험 경로 시작
                return Path.GetFullPath(TestDirectoryOverride); // 독립 시험 경로
            } // 시험 경로 끝
            return RunStorage.ResolveDirectory(); // 기존 회차와 시험 경로 공유
        } // 경로 선택 끝
        public GameSettingsStorage(string directory) // 독립 설정 경로 연결
        { // 연결 시작
            root = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); // 절대 경로 검증
            current = Path.Combine(root, "settings-v1.json"); // 현재 설정 파일 경로
        } // 연결 끝
        public GameSettings Load(out string warning) // 저장된 설정 또는 안전한 기본값
        { // 조회 시작
            warning = string.Empty; // 기본 안내 초기화
            loaded = true; // 최초 조회 상태
            blocked = false; // 이전 보호 상태 초기화
            expectedHash = null; // 이전 해시 초기화
            try // 현재 파일과 복구 후보 조회
            { // 조회 시도 시작
                if (File.Exists(current)) // 현재 파일 존재 검사
                { // 현재 파일 검사 시작
                    if (TryRead(current, out var value, out var hash)) // 파일 무결성 검사
                    { // 정상 파일 시작
                        expectedHash = hash; // 동시 변경 기준
                        return value; // 검증된 설정 반환
                    } // 정상 파일 끝
                    blocked = true; // 손상 원본 보호
                } // 현재 파일 검사 끝
                if (Directory.Exists(root)) // 복구 폴더 검사
                { // 복구 검사 시작
                    var paths = new List<string>(Directory.GetFiles(root, "settings-*.tmp")); // 중단된 임시 파일
                    paths.AddRange(Directory.GetFiles(root, "settings-backup-*.json")); // 이전 설정 백업
                    paths.Sort((left, right) => string.CompareOrdinal(Path.GetFileName(right), Path.GetFileName(left))); // 동일 종류의 최근 파일 우선
                    paths.Sort((left, right) => string.CompareOrdinal(CandidateKey(right), CandidateKey(left))); // 저장 시도 시간 순서
                    foreach (var path in paths) // 복구 후보 순회
                    { // 후보 검사 시작
                        if (TryRead(path, out var recovered, out _)) // 후보 무결성 검사
                        { // 복구 가능 시작
                            blocked = true; // 자동 덮어쓰기 차단
                            warning = "설정 복구 파일을 읽었습니다. 원본 확인 전에는 저장할 수 없습니다."; // 복구 보호 안내
                            return recovered; // 검증된 복구 설정
                        } // 복구 가능 끝
                    } // 후보 검사 끝
                    blocked = blocked || paths.Count > 0; // 손상 후보 보호
                } // 복구 검사 끝
                if (blocked) // 손상 파일 존재 검사
                { // 손상 시작
                    warning = "설정 파일을 읽을 수 없습니다. 기존 파일을 보존합니다."; // 원본 보호 안내
                } // 손상 끝
                return new GameSettings(100, 0); // 최초 기본 설정
            } // 조회 시도 끝
            catch (Exception) // 파일 접근 오류
            { // 접근 오류 시작
                blocked = true; // 저장 차단
                warning = "설정 파일을 읽을 수 없습니다. 기존 파일을 보존합니다."; // 파일 오류 안내
                return new GameSettings(100, 0); // 안전한 기본 설정
            } // 접근 오류 끝
        } // 조회 끝
        public bool TrySave(GameSettings settings, out string error) // 설정의 안전한 저장
        { // 저장 시작
            error = string.Empty; // 기본 오류 없음
            if (!loaded || blocked || settings == null || !GameSettings.IsValid(settings.MasterVolume, settings.EncounterFlash)) // 저장 상태와 값 검사
            { // 거절 시작
                error = "설정을 저장할 수 없습니다. 기존 파일을 확인해 주세요."; // 입력 오류 안내
                return false; // 안전하지 않은 저장 차단
            } // 거절 끝
            var replaced = false; // 원본 교체 여부
            try // 잠금과 원자 저장
            { // 저장 시도 시작
                Directory.CreateDirectory(root); // 설정 폴더 준비
                using (var gate = new FileStream(Path.Combine(root, "settings.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 설정 독점 잠금
                { // 잠금 시작
                    var exists = File.Exists(current); // 현재 파일 존재
                    if (exists != (expectedHash != null)) // 파일 생성 또는 삭제 충돌
                    { // 충돌 시작
                        blocked = true; // 추가 저장 차단
                        error = "설정 파일이 변경되었습니다. 다시 확인해 주세요."; // 충돌 안내
                        return false; // 기존 데이터 보호
                    } // 충돌 끝
                    if (exists && (!TryRead(current, out _, out var hash) || hash != expectedHash)) // 현재 파일 변경 검사
                    { // 변경 시작
                        blocked = true; // 추가 저장 차단
                        error = "설정 파일이 변경되었습니다. 다시 확인해 주세요."; // 변경 안내
                        return false; // 다른 설정 보존
                    } // 변경 끝
                    var payload = JsonUtility.ToJson(new Payload { version = 1, masterVolume = settings.MasterVolume, encounterFlash = settings.EncounterFlash }); // 설정 본문 직렬화
                    var envelope = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(Encoding.UTF8.GetBytes(payload)) }); // 체크섬 포함 외피
                    var attempt = DateTime.UtcNow.Ticks.ToString("D19") + "-" + Guid.NewGuid().ToString("N"); // 저장 시도 고유 이름
                    var temporary = Path.Combine(root, "settings-" + attempt + ".tmp"); // 같은 폴더 임시 파일
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // 독립 임시 파일 생성
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) // 문자 저장기 생성
                    { // 파일 기록 시작
                        writer.Write(envelope); // 설정 파일 기록
                        writer.Flush(); // 문자 버퍼 저장
                        stream.Flush(true); // 저장 장치 반영
                    } // 파일 기록 끝
                    if (!TryRead(temporary, out var candidate, out _) || candidate.MasterVolume != settings.MasterVolume || candidate.EncounterFlash != settings.EncounterFlash) // 임시 파일 검증
                    { // 검증 실패 시작
                        error = "설정을 검증하지 못했습니다."; // 검증 실패 안내
                        return false; // 현재 설정 보존
                    } // 검증 실패 끝
                    if (exists) // 기존 파일 검사
                    { // 교체 시작
                        File.Copy(current, Path.Combine(root, "settings-backup-" + attempt + ".json"), false); // 원본 별도 백업
                        File.Replace(temporary, current, null); // 원자적 파일 교체
                    } // 교체 끝
                    else // 최초 저장 분기
                    { // 최초 저장 시작
                        File.Move(temporary, current); // 원자적 파일 배치
                    } // 최초 저장 끝
                    replaced = true; // 파일 배치 완료
                    if (!TryRead(current, out var written, out var writtenHash) || written.MasterVolume != settings.MasterVolume || written.EncounterFlash != settings.EncounterFlash) // 최종 파일 재검증
                    { // 검증 실패 시작
                        blocked = true; // 추가 변경 차단
                        error = "저장한 설정을 검증하지 못했습니다."; // 검증 실패 안내
                        return false; // 미확인 저장 거절
                    } // 검증 실패 끝
                    expectedHash = writtenHash; // 다음 저장 충돌 기준
                    return true; // 확인된 저장 완료
                } // 잠금 끝
            } // 저장 시도 끝
            catch (Exception) // 잠금 또는 파일 오류
            { // 오류 시작
                blocked = blocked || replaced; // 교체 이후 확인 불가 보호
                error = "설정을 저장하지 못했습니다. 저장 공간과 권한을 확인해 주세요."; // 저장 실패 안내
                return false; // 기존 사용 값 유지 요청
            } // 오류 끝
        } // 저장 끝
        private static string CandidateKey(string path) // 임시와 백업의 저장 시도 키
        { // 키 추출 시작
            var name = Path.GetFileName(path); // 후보 파일 이름
            var prefix = name.StartsWith("settings-backup-", StringComparison.Ordinal) ? "settings-backup-" : "settings-"; // 후보 접두사
            return name.Substring(prefix.Length); // 시각과 고유 식별자
        } // 키 추출 끝
        private static bool TryRead(string path, out GameSettings settings, out string hash) // 무결성 포함 설정 파일 조회
        { // 조회 시작
            settings = null; // 기본 미확인 값
            hash = null; // 기본 해시 없음
            try // 파일 읽기
            { // 읽기 시도 시작
                var bytes = File.ReadAllBytes(path); // 전체 파일 원본
                var envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(bytes)); // 저장 외피 해석
                if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.checksum) || Hash(Encoding.UTF8.GetBytes(envelope.payload)) != envelope.checksum) // 본문 체크섬 검사
                { // 손상 시작
                    return false; // 손상된 파일 거절
                } // 손상 끝
                var payload = JsonUtility.FromJson<Payload>(envelope.payload); // 설정 본문 해석
                if (payload == null || payload.version != 1 || !GameSettings.IsValid(payload.masterVolume, payload.encounterFlash)) // 버전과 값 검사
                { // 유효성 오류 시작
                    return false; // 잘못된 설정 거절
                } // 유효성 오류 끝
                settings = new GameSettings(payload.masterVolume, payload.encounterFlash); // 검증된 독립 설정
                hash = Hash(bytes); // 원본 변경 검사 값
                return true; // 정상 파일 확인
            } // 읽기 시도 끝
            catch (Exception) // 파일 접근 또는 해석 오류
            { // 오류 시작
                return false; // 검증되지 않은 파일 거절
            } // 오류 끝
        } // 조회 끝
        private static string Hash(byte[] bytes) // SHA256 무결성 값
        { // 계산 시작
            using (var sha = SHA256.Create()) // 해시 계산기 생성
            { // 계산기 사용 시작
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty); // 대문자 해시 변환
            } // 계산기 사용 끝
        } // 계산 끝
    } // 저장소 끝
} // 이름 공간 끝
