using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 47일차: 전투 참가자 한 명의 일러스트·이름·체력바를 표시하는 슬롯 뷰.
    // 적 슬롯 4개와 플레이어 상태 일러스트가 같은 컴포넌트를 재사용한다.
    // 49일차: 대상 선택을 위해 슬롯 클릭·선택 가능·선택됨 표시를 추가했다.
    [DisallowMultipleComponent]
    public sealed class BattleParticipantSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject slotRoot;
        [SerializeField] private Text slotIndexText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Button clickButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject defendBadge; // 52일차: 방어 중 표시

        [Header("Portrait")]
        [SerializeField] private Color aliveTint =
            Color.white;

        [SerializeField] private Color defeatedTint =
            new Color(0.35f, 0.35f, 0.42f, 0.65f);

        // 일러스트가 아직 없는 슬롯에 표시할 자리 표시 색상.
        [SerializeField] private Color emptyPortraitColor =
            new Color(0.18f, 0.20f, 0.26f, 1f);

        [Header("Selection (49일차)")]
        [SerializeField] private Color normalBackgroundColor =
            new Color(0.09f, 0.10f, 0.14f, 0.92f);

        [SerializeField] private Color selectableBackgroundColor =
            new Color(0.16f, 0.20f, 0.16f, 0.92f);

        [SerializeField] private Color selectedBackgroundColor =
            new Color(0.30f, 0.62f, 0.32f, 0.95f);

        public bool HasBoundParticipant { get; private set; }

        private bool isSelected;

        public void SetSlotLabel(
            string label)
        {
            if (slotIndexText != null)
            {
                slotIndexText.text =
                    label;
            }
        }

        public void Bind(
            BattleParticipant participant,
            Sprite portrait)
        {
            if (participant == null)
            {
                Clear();
                return;
            }

            HasBoundParticipant =
                true;

            SetSlotVisible(
                true);

            if (nameText != null)
            {
                nameText.text =
                    participant.DefinitionId;
            }

            ApplyPortrait(
                portrait,
                participant.IsAlive);

            ApplyHealth(
                participant.CurrentHp,
                participant.MaxHp);

            ApplyDefendBadge(
                participant.IsDefending);
        }

        public void Clear()
        {
            HasBoundParticipant =
                false;

            SetSlotVisible(
                false);

            SetSelectable(
                false);

            SetSelected(
                false);

            ApplyDefendBadge(
                false);
        }

        // 49일차: 이 슬롯을 대상으로 선택할 수 있는지 여부. 선택 가능할 때만 클릭이 동작한다.
        public void SetSelectable(
            bool selectable)
        {
            if (clickButton != null)
            {
                clickButton.interactable =
                    selectable;
            }

            if (backgroundImage != null
                && !isSelected)
            {
                backgroundImage.color =
                    selectable
                        ? selectableBackgroundColor
                        : normalBackgroundColor;
            }
        }

        // 49일차: 현재 선택된 대상인지 강조 표시한다.
        public void SetSelected(
            bool selected)
        {
            isSelected =
                selected;

            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.color =
                selected
                    ? selectedBackgroundColor
                    : (clickButton != null && clickButton.interactable
                        ? selectableBackgroundColor
                        : normalBackgroundColor);
        }

        // 49일차: 슬롯 클릭 시 호출할 콜백을 등록한다. 기존 콜백은 교체된다.
        public void SetOnClick(
            UnityAction callback)
        {
            if (clickButton == null)
            {
                return;
            }

            clickButton.onClick.RemoveAllListeners();

            if (callback != null)
            {
                clickButton.onClick.AddListener(
                    callback);
            }
        }

        private void ApplyPortrait(
            Sprite portrait,
            bool isAlive)
        {
            if (portraitImage == null)
            {
                return;
            }

            portraitImage.sprite =
                portrait;

            // 일러스트가 없으면 자리 표시 색상만 보여준다.
            if (portrait == null)
            {
                portraitImage.color =
                    emptyPortraitColor;

                return;
            }

            portraitImage.color =
                isAlive
                    ? aliveTint
                    : defeatedTint;
        }

        private void ApplyHealth(
            int currentHp,
            int maxHp)
        {
            float ratio =
                maxHp > 0
                    ? Mathf.Clamp01(
                        currentHp / (float)maxHp)
                    : 0f;

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount =
                    ratio;
            }

            if (healthText != null)
            {
                healthText.text =
                    $"HP {currentHp} / {maxHp}";
            }
        }

        // 52일차: 방어 중 배지를 참가자 상태에 맞춰 켜고 끈다.
        private void ApplyDefendBadge(
            bool isDefending)
        {
            if (defendBadge == null)
            {
                return;
            }

            if (defendBadge.activeSelf != isDefending)
            {
                defendBadge.SetActive(
                    isDefending);
            }
        }

        private void SetSlotVisible(
            bool visible)
        {
            if (slotRoot == null)
            {
                return;
            }

            if (slotRoot.activeSelf != visible)
            {
                slotRoot.SetActive(
                    visible);
            }
        }
    }
}
