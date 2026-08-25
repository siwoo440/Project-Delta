namespace ProjectDelta.Application
{
    // 59일차: 기획서 10.3 "던전 생성기는 전달받은 난수 인터페이스를 사용한다. UnityEngine.Random의
    // 전역 상태를 핵심 게임 판정에 직접 사용하지 않는다"에 대응하는 공통 계약.
    // 던전·조우·전투·이벤트·보상 난수(9.3 난수 분리 표)는 각자 이 인터페이스를 구현해 분리한다.
    // 지금은 CombatRng가 실제로 쓰는 NextInt만 정의한다. NextFloat·Shuffle·CaptureState(시드
    // 저장/복원)는 그 기능을 실제로 쓰는 시스템이 생길 때(전투 상태 저장이 붙는 일차 등) 추가한다.
    public interface IRandomSource
    {
        // minInclusive 이상 maxExclusive 미만의 정수 하나를 반환한다.
        int NextInt(int minInclusive, int maxExclusive);
    }
}
