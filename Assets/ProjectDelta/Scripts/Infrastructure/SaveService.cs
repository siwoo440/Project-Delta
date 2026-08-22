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
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        public void WriteProfile(ProfileData profile) => WriteFile(SavePaths.ProfilePath, profile, saveState: null);
        public ProfileData ReadProfile() => ReadFile<ProfileData>(SavePaths.ProfilePath);
        public bool HasProfile() => File.Exists(SavePaths.ProfilePath);

        public void WriteRun(RunData run, string saveState) => WriteFile(SavePaths.RunPath, run, saveState);
        public RunData ReadRun() => ReadFile<RunData>(SavePaths.RunPath);
        public bool HasRun() => File.Exists(SavePaths.RunPath);

        public void DeleteRun()
        {
            if (File.Exists(SavePaths.RunPath))
            {
                File.Delete(SavePaths.RunPath);
            }
        }

        public void WriteSettings(SettingsData settings) => WriteFile(SavePaths.SettingsPath, settings, saveState: null);
        public SettingsData ReadSettings() => ReadFile<SettingsData>(SavePaths.SettingsPath);
        public bool HasSettings() => File.Exists(SavePaths.SettingsPath);

        // 기획서 9.5절 "안전한 파일 쓰기":
        // 새 데이터 생성 → 임시 파일 기록 → 기록 완료 확인 → 체크섬 생성
        // → 임시 파일 다시 읽기 → 데이터 검증 → 기존 파일을 백업으로 이동
        // → 임시 파일을 현재 파일로 교체 → 저장 완료 표시
        //
        // 백업은 오늘은 슬롯 1개(.bak)까지만 유지한다. 최근 3개 순환 보관은 9일차.
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

            // 기록 완료 확인 + 임시 파일 다시 읽기 + 데이터 검증
            if (!TryReadEnvelope(tempPath, out _))
            {
                File.Delete(tempPath);
                throw new IOException($"저장 검증 실패: {targetPath}");
            }

            if (File.Exists(targetPath))
            {
                // 기존 파일을 백업으로 이동하면서 임시 파일로 원자적 교체
                File.Replace(tempPath, targetPath, targetPath + ".bak");
            }
            else
            {
                File.Move(tempPath, targetPath);
            }
        }

        private T ReadFile<T>(string path)
        {
            if (!TryReadEnvelope(path, out var envelope))
            {
                throw new InvalidDataException($"손상된 저장 파일: {path}");
            }

            return JsonConvert.DeserializeObject<T>(envelope.PayloadJson, JsonSettings);
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
