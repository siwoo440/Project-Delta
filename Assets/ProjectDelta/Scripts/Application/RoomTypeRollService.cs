using System;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 110일차: 새로 생성된 방의 종류를 굴린다. 가중치는 임의로 정했다.
    // Combat/Event는 아직 이 굴림에 참여하지 않는다 - 기존 몬스터 조우(40일차)와
    // 이벤트(107~109일차) 시스템이 RoomType과 독립적으로 동작하고 있어서,
    // 지금 섞으면 두 시스템이 같은 방에 중복 배정될 수 있다. 두 시스템이
    // RoomType을 참조하도록 연결되는 일차에 맞춰 가중치를 다시 설계해야 한다.
    public static class RoomTypeRollService
    {
        // 방 100개 중 15개 정도가 함정 방이 되는 정도의 임의 비율.
        private const int TrapChancePercent = 15;

        public static RoomType Roll(
            Random random = null)
        {
            Random rng =
                random
                ?? new Random();

            return rng.Next(
                100) < TrapChancePercent
                ? RoomType.Trap
                : RoomType.Normal;
        }
    }
}
