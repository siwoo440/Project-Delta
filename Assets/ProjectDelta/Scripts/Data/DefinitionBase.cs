using UnityEngine;

namespace ProjectDelta.Data
{
    // ID convention: <CATEGORY>_<NAME>, e.g. MON_SLIME, ITEM_HEAL_SMALL.
    // IDs are permanent once shipped; display names may change, IDs never do.
    public abstract class DefinitionBase : ScriptableObject
    {
        [SerializeField] private string id;

        public string Id => id;

        // 113일차: 런타임 테스트 정의도 정식 DefinitionTable 규칙을 사용할 수 있도록 ID 설정 통로를 제공한다.
        protected void SetRuntimeId(
            string value)
        {
            id =
                value;
        }
    }
}
