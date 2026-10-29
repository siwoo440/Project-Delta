using System; // 예외와 식별자
using System.IO; // 독립 시험 파일
using NUnit.Framework; // 저장 검증 도구
using ProjectDelta.Core; // 설정 값 규칙
using ProjectDelta.Runtime; // 설정 저장소
using UnityEngine; // 시험 프로젝트 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class GameSettingsStorageTests // 설정 저장 검증
    { // 검증 시작
        private string directory; // 독립 저장 폴더
        [SetUp] // 시험 폴더 준비
        public void Setup() // 독립 경로 지정
        { // 준비 시작
            directory = Path.Combine(Application.dataPath, "..", "Artifacts", "SettingsTests", Guid.NewGuid().ToString("N")); // 충돌 없는 시험 경로
            Directory.CreateDirectory(directory); // 시험 폴더 생성
        } // 준비 끝
        [TearDown] // 시험 폴더 정리
        public void Cleanup() // 독립 파일 제거
        { // 정리 시작
            if (Directory.Exists(directory)) // 시험 폴더 존재 검사
            { // 제거 시작
                Directory.Delete(directory, true); // 시험 전용 폴더 제거
            } // 제거 끝
        } // 정리 끝
        [Test] // 기본 설정과 범위 검증
        public void Settings_ValidateBoundsAndDefaults() // 기본값과 오류 입력
        { // 검증 시작
            var store = new GameSettingsStorage(directory); // 독립 설정 저장소
            var defaults = store.Load(out var warning); // 새 설정 조회
            Assert.That(warning, Is.Empty); // 정상 최초 조회
            Assert.That(defaults.MasterVolume, Is.EqualTo(100)); // 기본 전체 음량
            Assert.That(defaults.EncounterFlash, Is.Zero); // 기본 조우 섬광
            Assert.That(GameSettings.IsValid(0, 2), Is.True); // 하한과 상한 허용
            Assert.That(GameSettings.IsValid(101, 0), Is.False); // 음량 초과 거절
            Assert.That(GameSettings.IsValid(-1, 0), Is.False); // 음량 음수 거절
            Assert.That(GameSettings.IsValid(50, 3), Is.False); // 섬광 초과 거절
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSettings(50, -1)); // 잘못된 설정 생성 거절
        } // 검증 끝
        [Test] // 재시작 설정 복원 검증
        public void Save_RestoresExactValuesAfterRestart() // 독립 실행 복원
        { // 검증 시작
            var store = new GameSettingsStorage(directory); // 최초 저장소
            store.Load(out _); // 저장 전 기준 확인
            Assert.That(store.TrySave(new GameSettings(37, 2), out var error), Is.True, error); // 실제 설정 저장
            var restored = new GameSettingsStorage(directory).Load(out var warning); // 새 인스턴스 복원
            Assert.That(warning, Is.Empty); // 정상 저장 복원
            Assert.That(restored.MasterVolume, Is.EqualTo(37)); // 저장된 음량
            Assert.That(restored.EncounterFlash, Is.EqualTo(2)); // 저장된 섬광
        } // 검증 끝
        [Test] // 잠금과 동시 변경 검증
        public void Save_LockAndStaleWriterPreserveExistingValues() // 실패 시 기존 값 보존
        { // 검증 시작
            var first = new GameSettingsStorage(directory); // 첫 저장소
            var second = new GameSettingsStorage(directory); // 독립 동시 저장소
            first.Load(out _); // 첫 기준 조회
            second.Load(out _); // 두 번째 기준 조회
            Assert.That(first.TrySave(new GameSettings(25, 1), out var error), Is.True, error); // 최초 값 저장
            Assert.That(second.TrySave(new GameSettings(40, 2), out _), Is.False); // 오래된 기준 거절
            using (new FileStream(Path.Combine(directory, "settings.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 설정 거래 독점 잠금
            { // 잠금 시작
                Assert.That(first.TrySave(new GameSettings(70, 2), out _), Is.False); // 잠금 중 저장 거절
            } // 잠금 끝
            var value = new GameSettingsStorage(directory).Load(out _); // 실제 기존 저장 조회
            Assert.That(value.MasterVolume, Is.EqualTo(25)); // 기존 음량 보존
            Assert.That(value.EncounterFlash, Is.EqualTo(1)); // 기존 섬광 보존
        } // 검증 끝
        [Test] // 손상 파일과 원본 보존 검증
        public void CorruptFile_BlocksOverwriteAndPreservesBytes() // 손상 원본 보호
        { // 검증 시작
            var path = Path.Combine(directory, "settings-v1.json"); // 실제 설정 파일
            File.WriteAllText(path, "damaged settings"); // 손상 원본 생성
            var store = new GameSettingsStorage(directory); // 독립 저장소
            store.Load(out var warning); // 손상 파일 조회
            Assert.That(store.IsBlocked, Is.True); // 자동 덮어쓰기 차단
            Assert.That(warning, Is.Not.Empty); // 오류 안내 확인
            Assert.That(store.TrySave(new GameSettings(60, 1), out _), Is.False); // 손상 원본 보호
            Assert.That(File.ReadAllText(path), Is.EqualTo("damaged settings")); // 실제 원본 보존
        } // 검증 끝
        [Test] // 삭제 충돌과 백업 검증
        public void DeletedCurrent_RejectsOldWriterAndPreservesBackup() // 삭제된 현재 파일 보호
        { // 검증 시작
            var store = new GameSettingsStorage(directory); // 최초 저장소
            store.Load(out _); // 저장 기준 조회
            Assert.That(store.TrySave(new GameSettings(10, 0), out var error), Is.True, error); // 최초 값 기록
            Assert.That(store.TrySave(new GameSettings(20, 1), out error), Is.True, error); // 백업을 남기는 교체
            File.Delete(Path.Combine(directory, "settings-v1.json")); // 외부 현재 파일 삭제
            Assert.That(store.TrySave(new GameSettings(30, 2), out _), Is.False); // 삭제 이후 저장 차단
            var reload = new GameSettingsStorage(directory); // 독립 복원 조회
            var recovered = reload.Load(out var warning); // 보존된 백업 읽기
            Assert.That(reload.IsBlocked, Is.True); // 복구 파일의 자동 변경 차단
            Assert.That(warning, Is.Not.Empty); // 복구 안내
            Assert.That(recovered.MasterVolume, Is.EqualTo(10)); // 마지막 백업 값
        } // 검증 끝
    } // 검증 끝
} // 이름 공간 끝
