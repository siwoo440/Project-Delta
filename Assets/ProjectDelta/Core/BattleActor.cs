namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class BattleActor // 행동 순서용 전투 참가자
    { // 클래스 시작
        public string Name // 참가자 이름
        { // 속성 시작
            get; // 이름 조회
        } // 속성 끝
        public int Speed // 행동 순서 기준 속도
        { // 속성 시작
            get; // 속도 조회
        } // 속성 끝
        public bool IsPlayer // 플레이어 여부
        { // 속성 시작
            get; // 진영 조회
        } // 속성 끝
        public int Slot // 적의 슬롯 번호
        { // 속성 시작
            get; // 슬롯 조회
        } // 속성 끝
        public bool IsAlive // 생존 상태
        { // 속성 시작
            get; // 생존 여부 조회
            set; // 생존 여부 변경
        } // 속성 끝
        public BattleActor(string name, int speed, bool isPlayer, int slot, bool isAlive = true) // 시험 참가자 생성
        { // 생성 시작
            Name = name; // 참가자 이름 보관
            Speed = speed; // 속도 보관
            IsPlayer = isPlayer; // 참가자 진영 보관
            Slot = slot; // 슬롯 순서 보관
            IsAlive = isAlive; // 생존 상태 보관
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
