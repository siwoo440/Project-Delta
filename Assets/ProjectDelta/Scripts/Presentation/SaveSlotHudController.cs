using System;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 슬롯 하나에 필요한 UI 참조 묶음. 인스펙터에서 슬롯 개수만큼 배열로 채운다.
    [Serializable]
    public sealed class SaveSlotRowRefs
    {
        public Text slotLabelText;
        public Text savedTimeText;
        public Text playtimeText;

        // 저장 데이터가 없을 때 "저장 데이터 없음"을 표시하는 텍스트.
        public Text emptyStatusText;

        public Button saveButton;
        public Button loadButton;
    }

    // 109일차: 여러 저장 슬롯을 목록으로 보여주고 저장/불러오기를 지원하는 화면.
    // 슬롯 번호는 1부터 표시한다(배열 index 0 = 슬롯 1). 슬롯 0(기존 단일 저장 파일)은
    // 이 UI에는 노출하지 않는다 - 저장 슬롯 기능이 없던 시절의 자동 저장 호환용이다.
    [DisallowMultipleComponent]
    public sealed class SaveSlotHudController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private Button closeButton;

        [Header("Slots")]
        [SerializeField]
        private SaveSlotRowRefs[] rows =
            new SaveSlotRowRefs[2];

        private void Awake()
        {
            HookButtons();
            Close();
        }

        public void Open()
        {
            if (panel != null)
            {
                panel.SetActive(
                    true);
            }

            Refresh();
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(
                    false);
            }
        }

        private void HookButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();

                closeButton.onClick.AddListener(
                    Close);
            }

            if (rows == null)
            {
                return;
            }

            for (int index = 0;
                 index < rows.Length;
                 index++)
            {
                SaveSlotRowRefs row =
                    rows[index];

                if (row == null)
                {
                    continue;
                }

                int slot =
                    index + 1;

                if (row.saveButton != null)
                {
                    row.saveButton.onClick.RemoveAllListeners();

                    row.saveButton.onClick.AddListener(
                        () => OnSaveClicked(
                            slot));
                }

                if (row.loadButton != null)
                {
                    row.loadButton.onClick.RemoveAllListeners();

                    row.loadButton.onClick.AddListener(
                        () => OnLoadClicked(
                            slot));
                }
            }
        }

        private void Refresh()
        {
            if (rows == null)
            {
                return;
            }

            bool canSaveNow =
                ApplicationFlow.Current != null
                && RunContext.Current != null;

            for (int index = 0;
                 index < rows.Length;
                 index++)
            {
                SaveSlotRowRefs row =
                    rows[index];

                if (row == null)
                {
                    continue;
                }

                int slot =
                    index + 1;

                if (row.slotLabelText != null)
                {
                    row.slotLabelText.text =
                        $"슬롯 {slot}";
                }

                bool hasData =
                    ApplicationFlow.Current != null
                    && ApplicationFlow.Current.TryGetSlotSummary(
                        slot,
                        out SaveSlotSummary summary);

                if (row.savedTimeText != null)
                {
                    row.savedTimeText.text =
                        hasData
                            ? FormatSavedTime(
                                summary.SavedAtIso8601)
                            : string.Empty;
                }

                if (row.playtimeText != null)
                {
                    row.playtimeText.text =
                        hasData
                            ? FormatPlaytime(
                                summary.PlaytimeSeconds)
                            : string.Empty;
                }

                if (row.emptyStatusText != null)
                {
                    row.emptyStatusText.text =
                        hasData
                            ? string.Empty
                            : "저장 데이터 없음";

                    row.emptyStatusText.gameObject.SetActive(
                        !hasData);
                }

                if (row.loadButton != null)
                {
                    row.loadButton.interactable =
                        hasData;
                }

                if (row.saveButton != null)
                {
                    row.saveButton.interactable =
                        canSaveNow;
                }
            }
        }

        private void OnSaveClicked(
            int slot)
        {
            if (ApplicationFlow.Current == null)
            {
                return;
            }

            ApplicationFlow.Current.SaveToSlot(
                slot);

            Refresh();
        }

        private void OnLoadClicked(
            int slot)
        {
            if (ApplicationFlow.Current == null)
            {
                return;
            }

            // ContinueGame이 로딩 화면을 거쳐 던전 씬으로 전환하므로, 여기서는
            // 패널만 닫아두면 된다.
            Close();

            ApplicationFlow.Current.ContinueGame(
                slot);
        }

        private static string FormatSavedTime(
            string savedAtIso8601)
        {
            if (string.IsNullOrEmpty(
                    savedAtIso8601))
            {
                return string.Empty;
            }

            return DateTime.TryParse(
                savedAtIso8601,
                out DateTime parsed)
                ? parsed.ToLocalTime().ToString(
                    "yyyy-MM-dd HH:mm")
                : savedAtIso8601;
        }

        private static string FormatPlaytime(
            float playtimeSeconds)
        {
            TimeSpan span =
                TimeSpan.FromSeconds(
                    Math.Max(
                        0,
                        playtimeSeconds));

            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes}:{span.Seconds:00}";
        }
    }
}
