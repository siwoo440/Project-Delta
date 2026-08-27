using UnityEngine;

namespace ProjectDelta.Data
{
    // 104일차: 유물은 인벤토리 슬롯을 거치지 않고 획득 즉시 패시브가 적용되므로,
    // ItemDefinition을 확장하지 않고 별도 Definition으로 분리했다.
    [CreateAssetMenu(
        fileName = "RelicDefinition",
        menuName = "ProjectDelta/Data/Relic Definition")]
    public sealed class RelicDefinition : DefinitionBase
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        // 저주 유물 여부. 불리한 효과도 description에 전부 공개하는 것을 전제로 한다.
        [SerializeField]
        private bool isCursed;

        public string DisplayName =>
            displayName;

        public Sprite Icon =>
            icon;

        public string Description =>
            description;

        public bool IsCursed =>
            isCursed;
    }
}
