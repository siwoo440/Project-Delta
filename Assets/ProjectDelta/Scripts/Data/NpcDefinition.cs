using UnityEngine;

namespace ProjectDelta.Data
{
    // 113일차: NPC의 변하지 않는 기본 데이터만 보유한다.
    // 현재 호감도·조우 횟수·적대 상태는 Domain의 NpcRelationshipState가 담당한다.
    [CreateAssetMenu(fileName = "NpcDefinition", menuName = "ProjectDelta/Data/NPC Definition")]
    public sealed class NpcDefinition : DefinitionBase
    {
        [SerializeField] private string displayName;
        [SerializeField] private NpcServiceType serviceTypes = NpcServiceType.None;
        [SerializeField] private NpcHostilityMode hostilityMode = NpcHostilityMode.CanBecomeHostile;
        [SerializeField, Range(0, 100)] private int initialAffinity;
        [SerializeField] private bool persistentRelationship = true;

        [Header("적대 전환 시 전투 능력치")]
        [Min(1)] [SerializeField] private int maxHp = 20;
        [Min(0)] [SerializeField] private int attack = 5;
        [Min(0)] [SerializeField] private int defense = 3;
        [Min(0)] [SerializeField] private int speed = 5;
        [Min(0)] [SerializeField] private int charm = 5;
        [Min(0)] [SerializeField] private int evasion = 3;
        [Min(0)] [SerializeField] private int resistance = 3;

        public string DisplayName => displayName;
        public NpcServiceType ServiceTypes => serviceTypes;
        public NpcHostilityMode HostilityMode => hostilityMode;
        public int InitialAffinity => Mathf.Clamp(initialAffinity, 0, 100);
        public bool PersistentRelationship => persistentRelationship;
        public bool CanBattle => hostilityMode != NpcHostilityMode.Never;
        public bool StartsHostile => hostilityMode == NpcHostilityMode.StartsHostile;

        public int MaxHp => Mathf.Max(1, maxHp);
        public int Attack => Mathf.Max(0, attack);
        public int Defense => Mathf.Max(0, defense);
        public int Speed => Mathf.Max(0, speed);
        public int Charm => Mathf.Max(0, charm);
        public int Evasion => Mathf.Max(0, evasion);
        public int Resistance => Mathf.Max(0, resistance);

        // 113일차 테스트 NPC는 별도 asset/씬 연결 없이 런타임에서 이 설정 통로를 사용한다.
        public void ConfigureRuntime(
            string npcId,
            string npcDisplayName,
            NpcServiceType npcServiceTypes,
            NpcHostilityMode npcHostilityMode,
            int npcInitialAffinity)
        {
            SetRuntimeId(
                npcId);

            displayName =
                npcDisplayName;

            serviceTypes =
                npcServiceTypes;

            hostilityMode =
                npcHostilityMode;

            initialAffinity =
                Mathf.Clamp(
                    npcInitialAffinity,
                    0,
                    100);
        }
    }
}
