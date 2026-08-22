using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ProjectDelta.Data;

namespace ProjectDelta.Infrastructure
{
    public sealed class SaveService : ISaveService
    {
        private const int BackupSlotCount = 3;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public void WriteProfile(ProfileData profile) => WriteFile(SavePaths.ProfilePath, profile, saveState: null);
        public ProfileData ReadProfile() => ReadFileWithRecovery<ProfileData>(SavePaths.ProfilePath);
        public bool HasProfile() => File.Exists(SavePaths.ProfilePath);

        public void WriteRun(RunData run, string saveState) => WriteFile(SavePaths.RunPath, run, saveState);
        public RunData ReadRun() => ReadFileWithRecovery<RunData>(SavePaths.RunPath);
        public bool HasRun() => File.Exists(SavePaths.RunPath);

        public void DeleteRun()
        {
            if (File.Exists(SavePaths.RunPath))
            {
                File.Delete(SavePaths.RunPath);
            }
        }

        public void WriteSettings(SettingsData settings) => WriteFile(SavePaths.SettingsPath, settings, saveState: null);
        public SettingsData ReadSettings() => ReadFileWithRecovery<SettingsData>(SavePaths.SettingsPath);
        public bool HasSettings() => File.Exists(SavePaths.SettingsPath);

        // 기획서 9.5절 "안전한 파일 쓰기" + "자동 백업"(최근 3개 순환).
        private void WriteFile<T>(string targetPath, T payload, string saveState)
        {
            SavePaths.EnsureSaveDirectoryExists();

            var now = DateTime.UtcNow.ToString("o");
            var payloadJson = JsonConvert.SerializeObject(payload, JsonSettings);
            var envelope = new SaveEnvelope
            {
                GameVersion = UnityEngine.Application.version,
                CreatedAtIso8601 = now,
                ModifiedAtIso8601 = now,
                Platform = UnityEngine.Application.platform.ToString(),
                SaveState = saveState,
                Checksum = ComputeChecksum(payloadJson),
                PayloadJson = payloadJson
            };

            var tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(envelope, JsonSettings));

            if (!TryReadEnvelope(tempPath, out _))
            {
                File.Delete(tempPath);
                throw new IOException($"저장 검증 실패: {targetPath}");
            }

            if (File.Exists(targetPath))
            {
                RotateBackups(targetPath);
                // File.Replace가 "기존 파일을 Backup1로 이동 + 임시 파일로 교체"를 원자적으로 처리한다.
                File.Replace(tempPath, targetPath, SavePaths.GetBackupPath(targetPath, 1));
            }
            else
            {
                File.Move(tempPath, targetPath);
            }
        }

        // Backup2 → Backup3, Backup1 → Backup2로 밀어낸다.
        // 현재 파일 → Backup1 이동은 File.Replace가 대신 처리한다.
        private static void RotateBackups(string targetPath)
        {
            for (var slot = BackupSlotCount; slot >= 2; slot--)
            {
                var source = SavePaths.GetBackupPath(targetPath, slot - 1);
                var destination = SavePaths.GetBackupPath(targetPath, slot);

                if (File.Exists(source))
                {
                    File.Copy(source, destination, overwrite: true);
                }
            }
        }

        // 기획서 9.5절 "강제 종료 복구" 순서: 현재 파일 → 임시 파일 → Backup1 → Backup2 → Backup3.
        // 플레이어가 백업을 선택해 과거 시점으로 되돌아갈 수는 없다 - 손상 시에만 자동으로 사용한다.
        private T ReadFileWithRecovery<T>(string targetPath)
        {
            var candidates = new[]
            {
                targetPath,
                targetPath + ".tmp",
                SavePaths.GetBackupPath(targetPath, 1),
                SavePaths.GetBackupPath(targetPath, 2),
                SavePaths.GetBackupPath(targetPath, 3)
            };

            foreach (var candidate in candidates)
            {
                if (!File.Exists(candidate) || !TryReadEnvelope(candidate, out var envelope))
                {
                    continue;
                }

                if (candidate != targetPath)
                {
                    // TODO: UI 시스템이 생기면 9.5절 복구 안내 문구("저장 데이터에 문제가 발견되어
                    // 최근 정상 데이터로 복구했습니다")를 여기서 표시한다.
                    UnityEngine.Debug.LogWarning($"[ProjectDelta] 저장 데이터가 손상되어 백업에서 복구했습니다: {candidate}");
                }

                return JsonConvert.DeserializeObject<T>(envelope.PayloadJson, JsonSettings);
            }

            throw new InvalidDataException($"복구 실패: 정상적인 저장 데이터를 찾을 수 없습니다 ({targetPath})");
        }

        private static bool TryReadEnvelope(string path, out SaveEnvelope envelope)
        {
            envelope = JsonConvert.DeserializeObject<SaveEnvelope>(File.ReadAllText(path), JsonSettings);

            return envelope != null
                   && envelope.PayloadJson != null
                   && envelope.Checksum == ComputeChecksum(envelope.PayloadJson);
        }

        private static string ComputeChecksum(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
