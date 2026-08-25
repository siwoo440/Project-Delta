using ProjectDelta.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 55일차: 공격할 때마다 실제로 어떤 피해 공식·편차 난수가 적용됐는지 화면에서 바로
    // 확인하기 위한 디버그 전용 창. 정식 전투 화면(BattleHudController)과는 별개다.
    [DisallowMultipleComponent]
    public sealed class BattleDamageDebugOverlay : MonoBehaviour
    {
        [SerializeField] private ExplorationMonsterEncounterController encounterController;
        [SerializeField] private bool isVisible = true;

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
            // F9로 켜고 끌 수 있게 해, 스크린샷·녹화 때는 숨길 수 있다.
            // 프로젝트가 새 Input System을 쓰므로(레거시 UnityEngine.Input은 예외 발생) Keyboard.current로 읽는다.
            if (Keyboard.current != null
                && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                isVisible = !isVisible;
            }
        }

        private void OnGUI()
        {
            if (!isVisible
                || encounterController == null
                || string.IsNullOrEmpty(encounterController.LastDamageFormulaDebugText))
            {
                return;
            }

            const float width = 420f;
            const float height = 60f;
            const float margin = 12f;

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
