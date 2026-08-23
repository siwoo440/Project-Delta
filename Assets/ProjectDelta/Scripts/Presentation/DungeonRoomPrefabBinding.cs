using System; // Serializable 사용
using ProjectDelta.Data; // RoomDefinition 사용
using UnityEngine; // SerializeField 사용

namespace ProjectDelta.Presentation
{
    [Serializable]
    public sealed class DungeonRoomPrefabBinding
    {
        [SerializeField] private RoomDefinition definition; // 논리 방 정의
        [SerializeField] private RoomView prefab; // 실제 배치할 RoomView 프리팹
        [SerializeField] private bool useAsEntry; // 시작 방 템플릿 여부
        [SerializeField] private bool includeInGenerationPool = true; // 일반 생성 풀 포함 여부

        public RoomDefinition Definition => definition;
        public RoomView Prefab => prefab;
        public bool UseAsEntry => useAsEntry;
        public bool IncludeInGenerationPool => includeInGenerationPool;
        public bool IsValid => definition != null && prefab != null;
    }
}
