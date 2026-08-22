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
        public static string RunPath => Path.Combine(SaveDirectory, RunFileName);
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
