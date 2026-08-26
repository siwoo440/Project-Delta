using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 55일차 피해 공식 확인용 디버그 창.
    // 84일차부터 정식 피해 피드백이 전투 슬롯에 표시되므로 기본 상태에서는 숨긴다.
    [DisallowMultipleComponent]
    public sealed class BattleDamageDebugOverlay : MonoBehaviour
    {
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private bool isVisible = false;

        private void Awake()
        {
            if (encounterController == null)
            {
                encounterController =
                    GetComponent<ExplorationMonsterEncounterController>();
            }
        }

        private void Update()
        {
            if (Keyboard.current != null
                && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                isVisible =
                    !isVisible;
            }
        }

        private void OnGUI()
        {
            if (!isVisible
                || encounterController == null
                || string.IsNullOrEmpty(
                    encounterController.LastDamageFormulaDebugText))
            {
                return;
            }

            const float width =
                420f;

            const float height =
                60f;

            const float margin =
                12f;

            Rect boxRect =
                new Rect(
                    margin,
                    margin,
                    width,
                    height);

            GUI.Box(
                boxRect,
                "55일차 피해 공식 디버그 (F9로 숨기기)");

            Rect labelRect =
                new Rect(
                    boxRect.x + 8f,
                    boxRect.y + 20f,
                    boxRect.width - 16f,
                    boxRect.height - 24f);

            GUI.Label(
                labelRect,
                encounterController.LastDamageFormulaDebugText);
        }
    }
}
