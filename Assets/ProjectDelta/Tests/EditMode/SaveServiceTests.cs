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
    }
}
