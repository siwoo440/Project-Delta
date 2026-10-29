using System; // 고유 시험 경로
using System.IO; // 저장 파일 검사
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 회차 데이터
using ProjectDelta.Runtime; // 회차 저장 기능
using UnityEngine; // 독립 시험 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RunStorageTests // 회차 저장 검증
    { // 클래스 시작
        private string root; // 독립 저장 경로
        [SetUp] // 각 검증의 독립 경로
        public void Prepare() // 시험 경로 준비
        { // 준비 시작
            root = Path.Combine(Application.dataPath, "..", "Artifacts", "RunStorageTests", Guid.NewGuid().ToString("N")); // 고유 시험 폴더
        } // 준비 끝
        private static RunSaveData Snapshot(int hp) // 유효한 시험 회차 데이터
        { // 생성 시작
            var layout = DungeonGenerator.Generate(123); // 시험 던전 구조
            return new RunSaveData { version = 1, seed = 123, floor = 1, health = hp, maxHealth = 100, slots = new InventoryState().SaveSlots(), visitedRooms = new[] { new SavedRoom() }, doorLock = new DoorLockState(layout).Save(), history = new string[0] }; // 복원 가능한 시험 회차
        } // 생성 끝
        [Test] // 저장과 재읽기 검증
        public void Save_ReloadsRunFromIndependentInstance() // 새 저장 객체의 회차 읽기
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            Assert.That(store.Load(out _), Is.Null); // 최초 저장 없음
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 회차 저장 성공
            var loaded = new RunStorage(root).Load(out var warning); // 새 실행의 저장 조회
            Assert.That(warning, Is.Empty); // 정상 저장 안내 없음
            Assert.That(loaded.health, Is.EqualTo(42)); // 체력 복원 확인
        } // 검증 끝
        [Test] // 손상된 원본 보존 검증
        public void CorruptSave_IsNotOverwritten() // 손상 저장 덮어쓰기 거절
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            store.Load(out _); // 최초 저장 조회
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 정상 회차 저장
            var path = Path.Combine(root, "run-v1.json"); // 현재 회차 저장 경로
            File.WriteAllText(path, "corrupt"); // 손상 데이터 모사
            var fresh = new RunStorage(root); // 새 저장 기능
            Assert.That(fresh.Load(out var warning), Is.Null); // 손상 저장 읽기 거절
            Assert.That(warning, Is.Not.Empty); // 오류 안내 확인
            Assert.That(fresh.TrySave(Snapshot(50), out _), Is.False); // 손상 원본 덮어쓰기 차단
            Assert.That(File.ReadAllText(path), Is.EqualTo("corrupt")); // 원본 유지 확인
        } // 검증 끝
        [Test] // 동시 저장 충돌 검증
        public void ConcurrentChange_IsNotOverwritten() // 다른 실행의 변경 보존
        { // 검증 시작
            var first = new RunStorage(root); // 첫 실행 저장 기능
            var second = new RunStorage(root); // 두 번째 실행 저장 기능
            first.Load(out _); // 최초 상태 조회
            second.Load(out _); // 같은 최초 상태 조회
            Assert.That(first.TrySave(Snapshot(42), out _), Is.True); // 첫 실행의 저장
            Assert.That(second.TrySave(Snapshot(50), out _), Is.False); // 오래된 상태의 저장 거절
            Assert.That(new RunStorage(root).Load(out _).health, Is.EqualTo(42)); // 첫 실행의 기록 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
