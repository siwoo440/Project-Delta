namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum GamePhase // 게임 진행 상태
    { // 상태 목록 시작
        Initializing, // 초기 준비
        Loading, // 화면 전환 중
        Title, // 타이틀 화면
        Exploration, // 던전 탐험 화면
        Combat, // 시험 전투 화면
        Inventory, // 탐험 인벤토리 화면
        FullMap, // 전체 지도 화면
        History, // 현재 회차 기록 화면
        Encounter, // 조우 선택 화면
        Pause // 탐험 일시정지 메뉴
    } // 상태 목록 끝
} // 이름 공간 끝
