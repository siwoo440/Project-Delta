using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 113일차: 방 안의 NpcPoint 마커에 실제 NPC 정의와 관계 상태를 연결한다.
    public sealed class NpcContentMarker : MonoBehaviour
    {
        private NpcDefinition definition;
        private NpcRelationshipState relationshipState;

        public NpcDefinition Definition => definition;
        public NpcRelationshipState RelationshipState => relationshipState;

        public void Configure(
            NpcDefinition npcDefinition,
            NpcRelationshipState npcRelationshipState)
        {
            definition =
                npcDefinition;

            relationshipState =
                npcRelationshipState;
        }
    }
}
