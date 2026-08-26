using System;
using System.Collections;
using System.Collections.Generic;
using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 전투 참가자 한 명의 일러스트·체력·상태 및 행동 피드백 표시.
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
        [SerializeField] private GameObject defendBadge;

        [Header("Day 84 Runtime HUD")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text damageFeedbackText;
        [SerializeField] private Text currentActorText;

        [Header("Portrait")]
        [SerializeField] private Color aliveTint =
            Color.white;

        [SerializeField] private Color defeatedTint =
            new Color(0.35f, 0.35f, 0.42f, 0.65f);

        [SerializeField] private Color emptyPortraitColor =
            new Color(0.18f, 0.20f, 0.26f, 1f);

        [Header("Selection")]
        [SerializeField] private Color normalBackgroundColor =
            new Color(0.09f, 0.10f, 0.14f, 0.92f);

        [SerializeField] private Color selectableBackgroundColor =
            new Color(0.16f, 0.20f, 0.16f, 0.92f);

        [SerializeField] private Color selectedBackgroundColor =
            new Color(0.30f, 0.62f, 0.32f, 0.95f);

        [Header("Action Bump")]
        [SerializeField] private float bumpHeight = 14f;
        [SerializeField] private float bumpDuration = 0.12f;

        [Header("Day 84 Feedback")]
        [SerializeField] private float damageVisibleDuration = 0.65f;
        [SerializeField] private float damageFadeDuration = 0.25f;

        public bool HasBoundParticipant { get; private set; }

        private bool isSelected;
        private RectTransform portraitRectTransform;
        private Vector2 portraitRestPosition;
        private bool hasCachedPortraitRestPosition;
        private Coroutine bumpRoutine;
        private Coroutine damageFeedbackRoutine;
        private string boundParticipantInstanceId;
        private int lastHp;
        private bool hasHpSnapshot;

        private void Awake()
        {
            EnsureDay84RuntimeHud();
        }

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

            EnsureDay84RuntimeHud();

            bool sameParticipant =
                hasHpSnapshot
                && string.Equals(
                    boundParticipantInstanceId,
                    participant.InstanceId,
                    StringComparison.Ordinal);

            if (sameParticipant
                && lastHp != participant.CurrentHp)
            {
                string deltaText =
                    BattleHudDisplayFormatter.FormatVitalDelta(
                        lastHp,
                        participant.CurrentHp);

                if (!string.IsNullOrEmpty(
                        deltaText))
                {
                    ShowDamageFeedback(
                        deltaText);
                }
            }

            boundParticipantInstanceId =
                participant.InstanceId;

            lastHp =
                participant.CurrentHp;

            hasHpSnapshot =
                true;

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

            SetStatusEffects(
                participant.StatusEffects);
        }

        public void Clear()
        {
            HasBoundParticipant =
                false;

            boundParticipantInstanceId =
                null;

            hasHpSnapshot =
                false;

            SetSelectable(
                false);

            SetSelected(
                false);

            SetCurrentActor(
                false);

            SetStatusEffects(
                null);

            ApplyDefendBadge(
                false);

            ClearDamageFeedback();
            StopBump();
            SetSlotVisible(
                false);
        }

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
                    : (clickButton != null
                       && clickButton.interactable
                        ? selectableBackgroundColor
                        : normalBackgroundColor);
        }

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

        public void SetStatusEffects(
            IReadOnlyList<StatusEffectInstance> statusEffects)
        {
            EnsureDay84RuntimeHud();

            if (statusText == null)
            {
                return;
            }

            string formatted =
                BattleHudDisplayFormatter.FormatStatusEffects(
                    statusEffects);

            statusText.text =
                formatted;

            statusText.gameObject.SetActive(
                !string.IsNullOrEmpty(
                    formatted));
        }

        public void SetCurrentActor(
            bool isCurrentActor)
        {
            EnsureDay84RuntimeHud();

            if (currentActorText == null)
            {
                return;
            }

            currentActorText.text =
                isCurrentActor
                    ? "행동 중"
                    : string.Empty;

            currentActorText.gameObject.SetActive(
                isCurrentActor);
        }

        public void ShowDamageFeedback(
            string text)
        {
            EnsureDay84RuntimeHud();

            if (damageFeedbackText == null
                || string.IsNullOrEmpty(
                    text)
                || !isActiveAndEnabled)
            {
                return;
            }

            if (damageFeedbackRoutine != null)
            {
                StopCoroutine(
                    damageFeedbackRoutine);
            }

            damageFeedbackRoutine =
                StartCoroutine(
                    DamageFeedbackRoutine(
                        text));
        }

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

        private void EnsureDay84RuntimeHud()
        {
            Transform parent =
                slotRoot != null
                    ? slotRoot.transform
                    : transform;

            if (statusText == null)
            {
                statusText =
                    FindRuntimeText(
                        parent,
                        "Day84StatusText")
                    ?? CreateRuntimeText(
                        parent,
                        "Day84StatusText",
                        13,
                        FontStyle.Normal,
                        TextAnchor.LowerCenter);

                RectTransform rect =
                    statusText.rectTransform;

                rect.anchorMin =
                    new Vector2(
                        0f,
                        0f);

                rect.anchorMax =
                    new Vector2(
                        1f,
                        0f);

                rect.pivot =
                    new Vector2(
                        0.5f,
                        0f);

                rect.anchoredPosition =
                    new Vector2(
                        0f,
                        8f);

                rect.sizeDelta =
                    new Vector2(
                        -18f,
                        48f);

                statusText.horizontalOverflow =
                    HorizontalWrapMode.Wrap;

                statusText.verticalOverflow =
                    VerticalWrapMode.Truncate;

                statusText.gameObject.SetActive(
                    false);
            }

            if (damageFeedbackText == null)
            {
                damageFeedbackText =
                    FindRuntimeText(
                        parent,
                        "Day84DamageFeedback")
                    ?? CreateRuntimeText(
                        parent,
                        "Day84DamageFeedback",
                        27,
                        FontStyle.Bold,
                        TextAnchor.MiddleCenter);

                RectTransform rect =
                    damageFeedbackText.rectTransform;

                rect.anchorMin =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.anchorMax =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.pivot =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.anchoredPosition =
                    new Vector2(
                        0f,
                        44f);

                rect.sizeDelta =
                    new Vector2(
                        220f,
                        54f);

                Outline outline =
                    damageFeedbackText.GetComponent<Outline>();

                if (outline == null)
                {
                    outline =
                        damageFeedbackText.gameObject.AddComponent<Outline>();
                }

                outline.effectDistance =
                    new Vector2(
                        1f,
                        -1f);

                damageFeedbackText.gameObject.SetActive(
                    false);
            }

            if (currentActorText == null)
            {
                currentActorText =
                    FindRuntimeText(
                        parent,
                        "Day84CurrentActor")
                    ?? CreateRuntimeText(
                        parent,
                        "Day84CurrentActor",
                        14,
                        FontStyle.Bold,
                        TextAnchor.UpperCenter);

                RectTransform rect =
                    currentActorText.rectTransform;

                rect.anchorMin =
                    new Vector2(
                        0.5f,
                        1f);

                rect.anchorMax =
                    new Vector2(
                        0.5f,
                        1f);

                rect.pivot =
                    new Vector2(
                        0.5f,
                        1f);

                rect.anchoredPosition =
                    new Vector2(
                        0f,
                        -6f);

                rect.sizeDelta =
                    new Vector2(
                        120f,
                        24f);

                currentActorText.gameObject.SetActive(
                    false);
            }
        }

        private static Text FindRuntimeText(
            Transform parent,
            string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform found =
                parent.Find(
                    objectName);

            return found != null
                ? found.GetComponent<Text>()
                : null;
        }

        private static Text CreateRuntimeText(
            Transform parent,
            string objectName,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.transform.SetParent(
                parent,
                false);

            Text text =
                textObject.GetComponent<Text>();

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize =
                fontSize;

            text.fontStyle =
                fontStyle;

            text.alignment =
                alignment;

            text.raycastTarget =
                false;

            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            return text;
        }

        private void CachePortraitRestPositionIfNeeded()
        {
            if (hasCachedPortraitRestPosition
                || portraitImage == null)
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
                portraitRestPosition;

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

            float elapsed =
                0f;

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

        private IEnumerator DamageFeedbackRoutine(
            string text)
        {
            damageFeedbackText.text =
                text;

            Color baseColor =
                damageFeedbackText.color;

            baseColor.a =
                1f;

            damageFeedbackText.color =
                baseColor;

            damageFeedbackText.gameObject.SetActive(
                true);

            if (damageVisibleDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    damageVisibleDuration);
            }

            float elapsed =
                0f;

            while (elapsed < damageFadeDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float ratio =
                    damageFadeDuration > 0f
                        ? Mathf.Clamp01(
                            elapsed / damageFadeDuration)
                        : 1f;

                Color color =
                    baseColor;

                color.a =
                    1f - ratio;

                damageFeedbackText.color =
                    color;

                yield return null;
            }

            damageFeedbackText.gameObject.SetActive(
                false);

            damageFeedbackText.color =
                baseColor;

            damageFeedbackRoutine =
                null;
        }

        private void ClearDamageFeedback()
        {
            if (damageFeedbackRoutine != null)
            {
                StopCoroutine(
                    damageFeedbackRoutine);

                damageFeedbackRoutine =
                    null;
            }

            if (damageFeedbackText != null)
            {
                damageFeedbackText.text =
                    string.Empty;

                damageFeedbackText.gameObject.SetActive(
                    false);
            }
        }

        private void StopBump()
        {
            if (bumpRoutine == null)
            {
                return;
            }

            StopCoroutine(
                bumpRoutine);

            bumpRoutine =
                null;

            if (hasCachedPortraitRestPosition
                && portraitRectTransform != null)
            {
                portraitRectTransform.anchoredPosition =
                    portraitRestPosition;
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
