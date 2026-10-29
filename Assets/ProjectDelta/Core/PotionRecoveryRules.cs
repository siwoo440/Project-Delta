using System; // 최대 체력 범위 계산
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class PotionRecoveryRules // 회복약의 체력 회복 판정
    { // 클래스 시작
        public static bool TryRecover(int current, int maximum, int amount, out int recovered) // 현재 체력과 회복량 검사
        { // 판정 시작
            recovered = current; // 실패 시 원래 체력 보존
            if (maximum <= 0 || current <= 0 || current >= maximum || amount <= 0) // 유효한 체력과 회복 효과 검사
            { // 회복 불가 조건 시작
                return false; // 효과 없는 아이템 소비 차단
            } // 회복 불가 조건 끝
            recovered = (int)Math.Min((long)maximum, (long)current + amount); // 넘침 없이 최대 체력까지 회복
            return true; // 실제 체력 회복 성공
        } // 판정 끝
    } // 클래스 끝
} // 이름 공간 끝
