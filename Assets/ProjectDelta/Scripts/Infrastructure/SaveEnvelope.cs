using System;

namespace ProjectDelta.Infrastructure
{
    // Wraps every save payload with the common metadata fields (기획서 9.5절 저장 메타데이터).
    // The payload is kept as its own JSON string (not a nested object) so the
    // checksum can be verified against the exact bytes that were written,
    // instead of re-serializing an object graph and risking a false mismatch.
    [Serializable]
    public sealed class SaveEnvelope
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

        public string Checksum;
        public string PayloadJson;
    }
}
