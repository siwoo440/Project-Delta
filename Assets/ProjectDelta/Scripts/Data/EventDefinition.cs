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

    [Serializable]
    public sealed class EventChoiceDefinition
    {
        [SerializeField]
        [TextArea(1, 3)]
        private string choiceText;

        [SerializeField]
        private EventCondition[] conditions =
            Array.Empty<EventCondition>();

        public string ChoiceText =>
            choiceText;

        public IReadOnlyList<EventCondition> Conditions =>
            conditions
            ?? Array.Empty<EventCondition>();

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
