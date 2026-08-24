using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 47일차: 전투 참가자 한 명의 일러스트·이름·체력바를 표시하는 슬롯 뷰.
    // 적 슬롯 4개와 플레이어 상태 일러스트가 같은 컴포넌트를 재사용한다.
    [DisallowMultipleComponent]
    public sealed class BattleParticipantSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject slotRoot;
        [SerializeField] private Text slotIndexText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;

        [Header("Portrait")]
        [SerializeField] private Color aliveTint =
            Color.white;

        [SerializeField] private Color defeatedTint =
            new Color(0.35f, 0.35f, 0.42f, 0.65f);

        // 일러스트가 아직 없는 슬롯에 표시할 자리 표시 색상.
        [SerializeField] private Color emptyPortraitColor =
            new Color(0.18f, 0.20f, 0.26f, 1f);

        public bool HasBoundParticipant { get; private set; }

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
        }

        public void Clear()
        {
            HasBoundParticipant =
                false;

            SetSlotVisible(
                false);
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
