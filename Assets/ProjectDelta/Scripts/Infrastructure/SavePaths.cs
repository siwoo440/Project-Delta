using System.IO;

namespace ProjectDelta.Infrastructure
{
    // 저장 위치 규칙 (기획서 9.6절): 설치 폴더나 문서 폴더가 아닌
    // 플랫폼별 영구 데이터 경로(Application.persistentDataPath)를 사용한다.
    public static class SavePaths
    {
        private const string SaveFolderName = "Saves";
        private const string ProfileFileName = "profile.json";
        private const string RunFileName = "run.json";
        private const string SettingsFileName = "settings.json";

        // ProjectDelta.Application 네임스페이스와 이름이 겹쳐서 완전한 이름으로 명시한다.
        public static string SaveDirectory => Path.Combine(UnityEngine.Application.persistentDataPath, SaveFolderName);
        public static string ProfilePath => Path.Combine(SaveDirectory, ProfileFileName);

        // 슬롯 0은 기존 저장 파일명을 그대로 쓴다 - 저장 슬롯 UI가 생기기 전
        // 단일 저장 파일을 쓰던 플레이어의 데이터를 그대로 이어받기 위해서다.
        public static string RunPath => RunPathForSlot(0);

        public static string RunPathForSlot(int slot)
        {
            return slot <= 0
                ? Path.Combine(SaveDirectory, RunFileName)
                : Path.Combine(SaveDirectory, $"run_slot{slot}.json");
        }

        public static string SettingsPath => Path.Combine(SaveDirectory, SettingsFileName);

        public static void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        // 최근 3개 백업 순환 (기획서 9.5절 자동 백업). slot 1 = 직전 정상 저장.
        public static string GetBackupPath(string targetPath, int slot)
        {
            return $"{targetPath}.bak{slot}";
        }
    }
}
