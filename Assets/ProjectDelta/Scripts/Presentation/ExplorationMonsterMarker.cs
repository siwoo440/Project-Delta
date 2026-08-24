using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 41일차: 탐험 화면에 배치된 정지형 테스트 몬스터의 논리 위치 정보.
    // 이동 AI와 Encounter 접촉 판정은 이후 일차에서 이 정보를 사용한다.
    public sealed class ExplorationMonsterMarker : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private string monsterDefinitionId;
        [SerializeField] private int gridX;
        [SerializeField] private int gridZ;

        public string RoomId => roomId;
        public string MonsterDefinitionId => monsterDefinitionId;
        public GridPosition GridPosition =>
            new GridPosition(
                gridX,
                gridZ);

        public void Configure(
            string targetRoomId,
            string targetMonsterDefinitionId,
            GridPosition position)
        {
            roomId =
                targetRoomId;

            monsterDefinitionId =
                targetMonsterDefinitionId;

            gridX =
                position.X;

            gridZ =
                position.Z;
        }
    }
}
