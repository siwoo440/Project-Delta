using System; // 고유 시험 경로
using System.IO; // 시험 파일 검사
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 관계 상태 규칙
using ProjectDelta.Runtime; // 관계 저장소
using UnityEngine; // 격리된 프로젝트 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class NpcRelationshipStorageTests // 영구 관계 저장 검증
    { // 클래스 시작
        private string root; // 고유 시험 폴더
        private string current; // 현재 시험 저장 파일
        [SetUp] // 시험별 격리 준비
        public void Prepare() // 고유 폴더 생성
        { // 준비 시작
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "NpcRelationshipStorageTests", Guid.NewGuid().ToString("N"))); // 프로젝트 내부 고유 경로
            Directory.CreateDirectory(root); // 시험 폴더 생성
            current = Path.Combine(root, "npc-relationship-v1.json"); // 현재 파일 경로
        } // 준비 끝
        [TearDown] // 독립 시험 정리
        public void Cleanup() // 시험 폴더 제거
        { // 정리 시작
            if (Directory.Exists(root)) // 시험 폴더 존재 검사
            { // 폴더 조건 시작
                Directory.Delete(root, true); // 고유 시험 폴더만 제거
            } // 폴더 조건 끝
        } // 정리 끝
        private static NpcRelationshipState Grant(NpcRelationshipState state, string runId, string npcId) // 시험 우호 기록 생성
        { // 기록 생성 시작
            Assert.That(state.TryGrantFriendliness(runId, npcId, out var next), Is.True); // 정상 우호 적용 확인
            return next; // 변경된 독립 상태
        } // 기록 생성 끝
        [Test] // 재실행 복원 검증
        public void Save_ReloadsRelationshipsAndReceipts() // 관계와 수령 기록 보존
        { // 검증 시작
            var store = new NpcRelationshipStorage(root); // 첫 실행 저장소
            var state = store.Load(out var warning); // 빈 관계 조회
            Assert.That(warning, Is.Empty); // 초기 상태 안내 없음
            state = Grant(state, "run-1", "merchant"); // 첫 회차 우호 기록
            state = Grant(state, "run-2", "merchant"); // 두 번째 회차 우호 기록
            Assert.That(store.TrySave(state, out var error), Is.True); // 실제 저장 성공
            Assert.That(error, Is.Empty); // 저장 오류 없음
            var reopened = new NpcRelationshipStorage(root); // 재실행 저장소
            var restored = reopened.Load(out warning); // 저장 상태 복원
            Assert.That(warning, Is.Empty); // 정상 복원 안내 없음
            Assert.That(restored.GetAffection("merchant"), Is.EqualTo(2 * NpcRelationshipRules.TrialFriendlyGain)); // 호감도 복원 확인
            Assert.That(restored.HasReceipt("run-1", "merchant"), Is.True); // 첫 수령 복원 확인
            Assert.That(restored.HasReceipt("run-2", "merchant"), Is.True); // 두 번째 수령 복원 확인
            Assert.That(restored.TryGrantFriendliness("run-1", "merchant", out _), Is.False); // 중복 수령 차단 확인
            Assert.That(reopened.IsBlocked, Is.False); // 정상 저장 허용 상태
        } // 검증 끝
        [Test] // 외부 수정 충돌 검증
        public void Save_RejectsExternalChangesAndPreservesCurrent() // 두 실행의 덮어쓰기 방지
        { // 검증 시작
            var initial = new NpcRelationshipStorage(root); // 초기 저장소
            var state = Grant(initial.Load(out _), "run-1", "merchant"); // 최초 관계 생성
            Assert.That(initial.TrySave(state, out _), Is.True); // 초기 파일 저장
            var stale = new NpcRelationshipStorage(root); // 이전 상태 실행
            var oldState = stale.Load(out _); // 충돌 기준 파일 읽기
            var other = new NpcRelationshipStorage(root); // 외부 실행
            var updated = Grant(other.Load(out _), "run-2", "merchant"); // 외부 변경 적용
            Assert.That(other.TrySave(updated, out _), Is.True); // 외부 변경 저장
            var expected = File.ReadAllBytes(current); // 외부 저장 원본
            var conflicting = Grant(oldState, "run-3", "merchant"); // 이전 상태의 별도 변경
            Assert.That(stale.TrySave(conflicting, out var error), Is.False); // 변경된 파일 덮어쓰기 거절
            Assert.That(error, Is.Not.Empty); // 충돌 안내 확인
            Assert.That(stale.IsBlocked, Is.True); // 추가 저장 차단 확인
            Assert.That(File.ReadAllBytes(current), Is.EqualTo(expected)); // 외부 저장 원본 보존
            var restored = new NpcRelationshipStorage(root).Load(out _); // 최신 파일 재확인
            Assert.That(restored.HasReceipt("run-2", "merchant"), Is.True); // 외부 수령 기록 유지
            Assert.That(restored.HasReceipt("run-3", "merchant"), Is.False); // 충돌 변경 미반영
        } // 검증 끝
        [Test] // 손상 파일과 백업 복구 검증
        public void Load_CorruptCurrentRecoversBackupAndBlocksWrites() // 백업 복구와 원본 보존
        { // 검증 시작
            var store = new NpcRelationshipStorage(root); // 초기 저장소
            var first = Grant(store.Load(out _), "run-1", "merchant"); // 첫 우호 기록
            Assert.That(store.TrySave(first, out _), Is.True); // 첫 정상 저장
            var second = Grant(first, "run-2", "merchant"); // 두 번째 우호 기록
            Assert.That(store.TrySave(second, out _), Is.True); // 이전 상태 백업 생성
            var backups = Directory.GetFiles(root, "npc-relationship-backup-*.json"); // 생성된 백업 목록
            Assert.That(backups.Length, Is.EqualTo(1)); // 복구 대상 백업 확인
            var backupBytes = File.ReadAllBytes(backups[0]); // 백업 원본 보관
            File.WriteAllText(current, "damaged test data"); // 현재 파일 손상 모사
            var corrupt = File.ReadAllBytes(current); // 손상 원본 보관
            var reopened = new NpcRelationshipStorage(root); // 재실행 저장소
            var restored = reopened.Load(out var warning); // 정상 백업 복구
            Assert.That(warning, Is.Not.Empty); // 복구 안내 확인
            Assert.That(reopened.IsBlocked, Is.True); // 복구 상태 저장 차단
            Assert.That(restored.HasReceipt("run-1", "merchant"), Is.True); // 백업 수령 기록 복구
            Assert.That(restored.HasReceipt("run-2", "merchant"), Is.False); // 손상된 최신 수령 기록 제외
            Assert.That(reopened.TrySave(restored, out var error), Is.False); // 손상 원본 덮어쓰기 차단
            Assert.That(error, Is.Not.Empty); // 저장 차단 안내 확인
            Assert.That(File.ReadAllBytes(current), Is.EqualTo(corrupt)); // 손상 원본 보존
            Assert.That(File.ReadAllBytes(backups[0]), Is.EqualTo(backupBytes)); // 정상 백업 보존
        } // 검증 끝
        [Test] // 체크섬 손상 검증
        public void Load_RejectsTamperedPayloadWithoutBackup() // 조작된 원본 보호
        { // 검증 시작
            var store = new NpcRelationshipStorage(root); // 초기 저장소
            var state = Grant(store.Load(out _), "run-1", "merchant"); // 정상 우호 기록
            Assert.That(store.TrySave(state, out _), Is.True); // 정상 기록 저장
            File.WriteAllText(current, File.ReadAllText(current).Replace("merchant", "intruder")); // 체크섬 없는 본문 변경
            var corrupt = File.ReadAllBytes(current); // 변조 원본 보관
            var reopened = new NpcRelationshipStorage(root); // 재실행 저장소
            var restored = reopened.Load(out var warning); // 손상 기록 조회
            Assert.That(warning, Is.Not.Empty); // 손상 안내 확인
            Assert.That(reopened.IsBlocked, Is.True); // 저장 차단 확인
            Assert.That(restored.HasReceipt("run-1", "intruder"), Is.False); // 변조 기록 거절
            Assert.That(reopened.TrySave(restored, out _), Is.False); // 손상 파일 덮어쓰기 차단
            Assert.That(File.ReadAllBytes(current), Is.EqualTo(corrupt)); // 변조 원본 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
