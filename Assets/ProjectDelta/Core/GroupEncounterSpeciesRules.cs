namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class GroupEncounterSpeciesRules // 다수 조우 종족 선택 규칙
    { // 규칙 클래스 시작
        public static bool ForNewRun() // 신규 회차의 고블린 선택 여부
        { // 신규 회차 규칙 시작
            return true; // 신규 회차 고블린
        } // 신규 회차 규칙 끝
        public static bool FromSave(int version, bool groupGoblin) // 저장 버전별 고블린 선택 여부
        { // 저장 복원 규칙 시작
            return version >= 9 && groupGoblin; // 이전 저장 슬라임과 이후 저장 선택 복원
        } // 저장 복원 규칙 끝
    } // 규칙 클래스 끝
} // 이름 공간 끝
