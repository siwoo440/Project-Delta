using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDelta.Data
{
    // 107일차: 선택지 조건 4종. "판정, 결과, 보상"은 108~110일차에 걸쳐 확장될
    // 예정이라 이번에는 조건 검사에 필요한 최소 데이터만 정의한다.
    public enum EventConditionKind
    {
        None = 0,
        Stat = 1,
        Item = 2,
        Gold = 3,
        Flag = 4
    }

    // StatBlock의 9개 필드와 대응한다. Data 계층이 Domain의 StatBlock을 직접
    // 참조하지 않도록 이름만 같은 별도 enum으로 둔다.
    public enum EventStatType
    {
        MaxHealth = 0,
        MaxMana = 1,
        MaxStamina = 2,
        Attack = 3,
        Defense = 4,
        Speed = 5,
        Charm = 6,
        Evasion = 7,
        Resistance = 8
    }

    [Serializable]
    public sealed class EventCondition
    {
        [SerializeField]
        private EventConditionKind kind =
            EventConditionKind.None;

        // Stat 조건에서만 사용한다.
        [SerializeField]
        private EventStatType statType =
            EventStatType.Attack;

        // Item/Flag 조건에서 아이템 ID 또는 플래그 이름으로 쓴다.
        [SerializeField]
        private string targetId;

        // Stat/Item/Gold 조건의 최소 요구치. Flag 조건에서는 0이 아니면 "true를 요구"로 취급한다.
        [SerializeField]
        private int requiredValue;

        public EventConditionKind Kind =>
            kind;

        public EventStatType StatType =>
            statType;

        public string TargetId =>
            targetId
            ?? string.Empty;

        public int RequiredValue =>
            requiredValue;

        public bool RequiredFlagValue =>
            requiredValue != 0;

        public EventCondition()
        {
        }

        public EventCondition(
            EventConditionKind kind,
            string targetId,
            int requiredValue)
        {
            this.kind =
                kind;

            this.targetId =
                targetId;

            this.requiredValue =
                requiredValue;
        }

        public EventCondition(
            EventConditionKind kind,
            EventStatType statType,
            int requiredValue)
            : this(
                kind,
                (string)null,
                requiredValue)
        {
            this.statType =
                statType;
        }
    }

    // 108일차: 선택 결과 효과 종류. 조건(EventConditionKind)과 달리 결과는
    // 양방향(회복뿐 아니라 피해·소비도 가능)이라 값에 음수를 허용한다.
    public enum EventEffectKind
    {
        None = 0,
        RestoreHp = 1,
        RestoreMana = 2,
        RestoreStamina = 3,
        GainGold = 4,
        GainItem = 5,
        SetFlag = 6,

        // 관계(호감도) 시스템은 아직 없다(113일차 이후 예정). 데이터 자리만
        // 미리 만들어두고, 실제 적용은 EventResultService에서 no-op으로 둔다.
        RelationshipChange = 7
    }

    [Serializable]
    public sealed class EventEffect
    {
        [SerializeField]
        private EventEffectKind kind =
            EventEffectKind.None;

        // GainItem/SetFlag/RelationshipChange에서 아이템 ID·플래그 이름·NPC ID로 쓴다.
        [SerializeField]
        private string targetId;

        // GainItem에서만 사용하는 표시 이름 (인벤토리에 새로 추가될 수 있으므로 필요).
        [SerializeField]
        private string displayName;

        // 회복량/피해량, 골드 증감, 아이템 수량 증감에 쓴다. 음수 허용.
        [SerializeField]
        private int value;

        // SetFlag에서만 사용한다.
        [SerializeField]
        private bool flagValue;

        public EventEffectKind Kind =>
            kind;

        public string TargetId =>
            targetId
            ?? string.Empty;

        public string DisplayName =>
            string.IsNullOrEmpty(
                displayName)
                ? TargetId
                : displayName;

        public int Value =>
            value;

        public bool FlagValue =>
            flagValue;

        public EventEffect()
        {
        }

        public EventEffect(
            EventEffectKind kind,
            int value)
        {
            this.kind =
                kind;

            this.value =
                value;
        }

        public EventEffect(
            EventEffectKind kind,
            string targetId,
            int value,
            string displayName = null)
        {
            this.kind =
                kind;

            this.targetId =
                targetId;

            this.value =
                value;

            this.displayName =
                displayName;
        }

        public EventEffect(
            EventEffectKind kind,
            string targetId,
            bool flagValue)
        {
            this.kind =
                kind;

            this.targetId =
                targetId;

            this.flagValue =
                flagValue;
        }
    }

    [Serializable]
    public sealed class EventChoiceDefinition
    {
        [SerializeField]
        [TextArea(1, 3)]
        private string choiceText;

        [SerializeField]
        private EventCondition[] conditions =
            Array.Empty<EventCondition>();

        // 108일차: 이 선택지를 확정했을 때 적용되는 결과 목록.
        [SerializeField]
        private EventEffect[] results =
            Array.Empty<EventEffect>();

        public string ChoiceText =>
            choiceText;

        public IReadOnlyList<EventCondition> Conditions =>
            conditions
            ?? Array.Empty<EventCondition>();

        public IReadOnlyList<EventEffect> Results =>
            results
            ?? Array.Empty<EventEffect>();

        public EventChoiceDefinition()
        {
        }

        public EventChoiceDefinition(
            string choiceText,
            params EventCondition[] conditions)
        {
            this.choiceText =
                choiceText;

            this.conditions =
                conditions
                ?? Array.Empty<EventCondition>();
        }

        public EventChoiceDefinition(
            string choiceText,
            EventCondition[] conditions,
            EventEffect[] results)
        {
            this.choiceText =
                choiceText;

            this.conditions =
                conditions
                ?? Array.Empty<EventCondition>();

            this.results =
                results
                ?? Array.Empty<EventEffect>();
        }
    }

    [CreateAssetMenu(
        fileName = "EventDefinition",
        menuName = "ProjectDelta/Data/Event Definition")]
    public sealed class EventDefinition : DefinitionBase
    {
        [SerializeField]
        private string title;

        [SerializeField]
        [TextArea(3, 8)]
        private string body;

        [SerializeField]
        private EventChoiceDefinition[] choices =
            Array.Empty<EventChoiceDefinition>();

        public string Title =>
            title;

        public string Body =>
            body;

        public IReadOnlyList<EventChoiceDefinition> Choices =>
            choices
            ?? Array.Empty<EventChoiceDefinition>();
    }
}
