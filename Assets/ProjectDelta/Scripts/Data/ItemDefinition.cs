using UnityEngine;

namespace ProjectDelta.Data
{
    [CreateAssetMenu(
        fileName = "ItemDefinition",
        menuName = "ProjectDelta/Data/Item Definition")]
    public sealed class ItemDefinition : DefinitionBase
    {
        [SerializeField]
        private string displayName;

        // 89일차: 인벤토리 슬롯과 선택 아이템 영역에서 사용할 이미지다.
        [SerializeField]
        private Sprite icon;

        // 89일차: 선택한 아이템을 플레이어 UI 위에 표시할 때 사용할 설명이다.
        [SerializeField]
        [TextArea(2, 4)]
        private string description;

        public string DisplayName =>
            displayName;

        public Sprite Icon =>
            icon;

        public string Description =>
            description;
    }
}
