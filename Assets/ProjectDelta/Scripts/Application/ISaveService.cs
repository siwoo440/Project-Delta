using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // Domain systems call these methods instead of touching File I/O or JSON
    // directly (10.4절 원칙). Writes go through a temp-file-then-replace
    // sequence with checksum verification (기획서 9.5절 안전한 파일 쓰기);
    // reads reject files whose checksum doesn't match as corrupted.
    public interface ISaveService
    {
        void WriteProfile(ProfileData profile);
        ProfileData ReadProfile();
        bool HasProfile();

        void WriteRun(RunData run, string saveState);
        RunData ReadRun();
        bool HasRun();
        void DeleteRun();

        // 109일차: 저장 슬롯 UI용 슬롯 지정 API. slot 0은 기존 단일 저장과 같은 파일이다.
        void WriteRun(RunData run, string saveState, int slot);
        RunData ReadRun(int slot);
        bool HasRun(int slot);
        void DeleteRun(int slot);

        // RunData 전체를 읽지 않고 슬롯 카드에 필요한 요약 정보만 가져온다.
        // 저장된 데이터가 없으면 false를 반환한다.
        bool TryGetRunSummary(int slot, out SaveSlotSummary summary);

        void WriteSettings(SettingsData settings);
        SettingsData ReadSettings();
        bool HasSettings();
    }
}
