using System; // 유효하지 않은 입력 예외
namespace ProjectDelta.Core // 순수 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class GameSettings // 변경할 수 없는 설정 값
    { // 설정 시작
        public int MasterVolume { get; } // 전체 음량
        public int EncounterFlash { get; } // 조우 섬광 단계
        public GameSettings(int masterVolume, int encounterFlash) // 독립 설정 생성
        { // 생성 시작
            if (!IsValid(masterVolume, encounterFlash)) // 입력 범위 검사
            { // 오류 시작
                throw new ArgumentOutOfRangeException(nameof(masterVolume), "설정 값 범위를 확인해 주세요."); // 잘못된 설정 차단
            } // 오류 끝
            MasterVolume = masterVolume; // 전체 음량 보관
            EncounterFlash = encounterFlash; // 조우 섬광 보관
        } // 생성 끝
        public static bool IsValid(int masterVolume, int encounterFlash) // 설정 범위 검사
        { // 검사 시작
            return masterVolume >= 0 && masterVolume <= 100 && encounterFlash >= 0 && encounterFlash <= 2; // 허용 범위 확인
        } // 검사 끝
    } // 설정 끝
} // 이름 공간 끝
