using System.IO;
using NUnit.Framework;
using ProjectDelta.Data;
using ProjectDelta.Infrastructure;

namespace ProjectDelta.Tests.EditMode
{
    // 기획서 10.6절 EditMode 테스트 항목 "저장 버전 변환"에 대응하는
    // 저장·런타임 구간(4~9일차) 회귀 테스트.
    public class SaveServiceTests
    {
        private SaveService _saveService;

        [SetUp]
        public void SetUp()
        {
            _saveService = new SaveService();
            CleanSaveDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            CleanSaveDirectory();
        }

        private static void CleanSaveDirectory()
        {
            if (Directory.Exists(SavePaths.SaveDirectory))
            {
                Directory.Delete(SavePaths.SaveDirectory, recursive: true);
            }
        }

        [Test]
        public void WriteThenReadProfile_ReturnsEquivalentData()
        {
            var profile = new ProfileData();
            profile.PermanentGrowth.MemoryShards = 42;

            _saveService.WriteProfile(profile);
            var loaded = _saveService.ReadProfile();

            Assert.AreEqual(42, loaded.PermanentGrowth.MemoryShards);
        }

        [Test]
        public void SecondWrite_CreatesBackup1()
        {
            _saveService.WriteProfile(new ProfileData());
            _saveService.WriteProfile(new ProfileData());

            Assert.IsTrue(File.Exists(SavePaths.GetBackupPath(SavePaths.ProfilePath, 1)));
        }

        [Test]
        public void FourWrites_RotatesThreeBackups()
        {
            _saveService.WriteProfile(new ProfileData());
            _saveService.WriteProfile(new ProfileData());
            _saveService.WriteProfile(new ProfileData());
            _saveService.WriteProfile(new ProfileData());

            Assert.IsTrue(File.Exists(SavePaths.GetBackupPath(SavePaths.ProfilePath, 1)));
            Assert.IsTrue(File.Exists(SavePaths.GetBackupPath(SavePaths.ProfilePath, 2)));
            Assert.IsTrue(File.Exists(SavePaths.GetBackupPath(SavePaths.ProfilePath, 3)));
        }

        [Test]
        public void CorruptedCurrentFile_RecoversFromBackup1()
        {
            var profile = new ProfileData();
            profile.PermanentGrowth.MemoryShards = 7;
            _saveService.WriteProfile(profile);
            _saveService.WriteProfile(new ProfileData());

            File.WriteAllText(SavePaths.ProfilePath, "corrupted");

            var recovered = _saveService.ReadProfile();

            Assert.AreEqual(7, recovered.PermanentGrowth.MemoryShards);
        }

        [Test]
        public void AllCandidatesCorrupted_ThrowsInvalidDataException()
        {
            _saveService.WriteProfile(new ProfileData());
            File.WriteAllText(SavePaths.ProfilePath, "corrupted");

            Assert.Throws<InvalidDataException>(() => _saveService.ReadProfile());
        }

        // 109일차: 저장 슬롯 UI가 쓰는 슬롯 지정 API 회귀 테스트.
        [Test]
        public void WriteRun_ToSlot_ReadRunFromSameSlot_ReturnsData()
        {
            var run = new RunData();
            run.BasicInfo.RunId = "RUN_SLOT_1";

            _saveService.WriteRun(run, "InProgress", 1);
            var loaded = _saveService.ReadRun(1);

            Assert.AreEqual("RUN_SLOT_1", loaded.BasicInfo.RunId);
        }

        [Test]
        public void WriteRun_DifferentSlots_DoNotOverwriteEachOther()
        {
            var runSlot1 = new RunData();
            runSlot1.BasicInfo.RunId = "RUN_SLOT_1";

            var runSlot2 = new RunData();
            runSlot2.BasicInfo.RunId = "RUN_SLOT_2";

            _saveService.WriteRun(runSlot1, "InProgress", 1);
            _saveService.WriteRun(runSlot2, "InProgress", 2);

            Assert.AreEqual("RUN_SLOT_1", _saveService.ReadRun(1).BasicInfo.RunId);
            Assert.AreEqual("RUN_SLOT_2", _saveService.ReadRun(2).BasicInfo.RunId);
        }

        [Test]
        public void HasRun_Slot_ReflectsWriteAndDelete()
        {
            Assert.IsFalse(_saveService.HasRun(3));

            _saveService.WriteRun(new RunData(), "InProgress", 3);
            Assert.IsTrue(_saveService.HasRun(3));

            _saveService.DeleteRun(3);
            Assert.IsFalse(_saveService.HasRun(3));
        }

        [Test]
        public void DeleteRun_Slot_DoesNotAffectOtherSlots()
        {
            _saveService.WriteRun(new RunData(), "InProgress", 1);
            _saveService.WriteRun(new RunData(), "InProgress", 2);

            _saveService.DeleteRun(1);

            Assert.IsFalse(_saveService.HasRun(1));
            Assert.IsTrue(_saveService.HasRun(2));
        }

        [Test]
        public void TryGetRunSummary_NoData_ReturnsFalse()
        {
            bool found = _saveService.TryGetRunSummary(4, out SaveSlotSummary summary);

            Assert.IsFalse(found);
            Assert.IsFalse(summary.HasData);
            Assert.AreEqual(4, summary.Slot);
        }

        [Test]
        public void TryGetRunSummary_WithData_ReturnsRunIdAndSavedTime()
        {
            var run = new RunData();
            run.BasicInfo.RunId = "RUN_SUMMARY";
            run.BasicInfo.PlaytimeSeconds = 123f;

            _saveService.WriteRun(run, "InProgress", 5);

            bool found = _saveService.TryGetRunSummary(5, out SaveSlotSummary summary);

            Assert.IsTrue(found);
            Assert.IsTrue(summary.HasData);
            Assert.AreEqual("RUN_SUMMARY", summary.RunId);
            Assert.AreEqual(123f, summary.PlaytimeSeconds);
            Assert.IsFalse(string.IsNullOrEmpty(summary.SavedAtIso8601));
        }

        [Test]
        public void WriteRun_WithoutSlot_UsesSlotZeroPath()
        {
            var run = new RunData();
            run.BasicInfo.RunId = "RUN_LEGACY";

            _saveService.WriteRun(run, "InProgress");

            Assert.IsTrue(File.Exists(SavePaths.RunPath));
            Assert.AreEqual("RUN_LEGACY", _saveService.ReadRun(0).BasicInfo.RunId);
        }
    }
}
