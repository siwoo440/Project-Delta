using System.Collections;
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

        [Header("Action Bump (56일차)")]
        [SerializeField] private float bumpHeight = 14f;
        [SerializeField] private float bumpDuration = 0.12f; // 올라가는 시간, 내려오는 시간도 동일하게 사용

        public bool HasBoundParticipant { get; private set; }

        private bool isSelected;
        private RectTransform portraitRectTransform;
        private Vector2 portraitRestPosition;
        private bool hasCachedPortraitRestPosition;
        private Coroutine bumpRoutine;

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

            // 56일차: 슬롯이 비워지는 동안 이전 참가자의 튀어오르는 연출이 이어지지 않게 정지한다.
            if (bumpRoutine != null)
            {
                StopCoroutine(
                    bumpRoutine);

                bumpRoutine =
                    null;

                if (hasCachedPortraitRestPosition)
                {
                    portraitRectTransform.anchoredPosition =
                        portraitRestPosition;
                }
            }
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

        // 56일차: 적 턴이 버튼 없이 자동으로 진행돼 행동이 눈에 안 보이는 문제를 보완한다.
        // 이 슬롯의 참가자가 실제로 행동했을 때 일러스트를 살짝 위로 튀었다 내려오게 한다.
        public void PlayActionBump()
        {
            if (portraitImage == null
                || !isActiveAndEnabled)
            {
                return;
            }

            CachePortraitRestPositionIfNeeded();

            if (bumpRoutine != null)
            {
                StopCoroutine(
                    bumpRoutine);
            }

            bumpRoutine =
                StartCoroutine(
                    BumpRoutine());
        }

        private void CachePortraitRestPositionIfNeeded()
        {
            if (hasCachedPortraitRestPosition)
            {
                return;
            }

            portraitRectTransform =
                portraitImage.rectTransform;

            portraitRestPosition =
                portraitRectTransform.anchoredPosition;

            hasCachedPortraitRestPosition =
                true;
        }

        private IEnumerator BumpRoutine()
        {
            Vector2 upPosition =
                portraitRestPosition
                + new Vector2(
                    0f,
                    bumpHeight);

            yield return AnimatePortraitPosition(
                portraitRestPosition,
                upPosition,
                bumpDuration);

            yield return AnimatePortraitPosition(
                upPosition,
                portraitRestPosition,
                bumpDuration);

            portraitRectTransform.anchoredPosition =
                portraitRestPosition; // 부동소수점 오차로 원위치에서 살짝 어긋나는 것을 방지

            bumpRoutine =
                null;
        }

        private IEnumerator AnimatePortraitPosition(
            Vector2 from,
            Vector2 to,
            float duration)
        {
            if (duration <= 0f)
            {
                portraitRectTransform.anchoredPosition =
                    to;

                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / duration);

                portraitRectTransform.anchoredPosition =
                    Vector2.Lerp(
                        from,
                        to,
                        t);

                yield return null;
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
