using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 113일차: 방 안의 NpcPoint 마커에 실제 NPC 정의와 관계 상태를 연결한다.
    // 114일차: 서비스 상태(상인 재고 등)도 같은 마커에 붙여서 같은 층 재방문 시
    // 그대로 유지되게 한다 - NPC GameObject는 층 이동 전까지 파괴되지 않는다.
    public sealed class NpcContentMarker : MonoBehaviour
    {
        private NpcDefinition definition;
        private NpcRelationshipState relationshipState;
        private NpcServiceRunState serviceState;

        public NpcDefinition Definition => definition;
        public NpcRelationshipState RelationshipState => relationshipState;
        public NpcServiceRunState ServiceState => serviceState;

        public void Configure(
            NpcDefinition npcDefinition,
            NpcRelationshipState npcRelationshipState,
            NpcServiceRunState npcServiceState)
        {
            definition =
                npcDefinition;

            relationshipState =
                npcRelationshipState;

            serviceState =
                npcServiceState;
        }
    }
}
