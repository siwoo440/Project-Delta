using System;

namespace ProjectDelta.Infrastructure
{
    // Wraps every save payload with the common metadata fields (기획서 9.5절 저장 메타데이터).
    // Checksum is added once atomic file writing is implemented (8일차).
    [Serializable]
    public sealed class SaveEnvelope<TPayload>
    {
        public const int CurrentSaveVersion = 1;

        public int SaveVersion = CurrentSaveVersion;
        public string GameVersion;

        // TODO: 콘텐츠 데이터(Definition) 버전 체계가 생기면 채운다.
        public string ContentVersion;

        public string CreatedAtIso8601;
        public string ModifiedAtIso8601;
        public string Platform;

        // RunData 저장 시에만 의미 있음 (탐험/이벤트/전투 등). Profile/Settings는 비워둔다.
        public string SaveState;

        public TPayload Payload;

        // TODO: 실제 파일에 다시 쓸 때는 기존 CreatedAtIso8601을 보존해야 한다 (7일차 파일 I/O에서 처리).
        public static SaveEnvelope<TPayload> Wrap(TPayload payload, string saveState)
        {
            var now = DateTime.UtcNow.ToString("o");
            return new SaveEnvelope<TPayload>
            {
                GameVersion = UnityEngine.Application.version,
                CreatedAtIso8601 = now,
                ModifiedAtIso8601 = now,
                Platform = UnityEngine.Application.platform.ToString(),
                SaveState = saveState,
                Payload = payload
            };
        }
    }
}
