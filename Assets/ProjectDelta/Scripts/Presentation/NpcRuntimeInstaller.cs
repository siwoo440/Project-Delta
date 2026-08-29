using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectDelta.Presentation
{
    // 113일차: 씬을 직접 수정하지 않아도 DungeonScene의 Player에 NPC 테스트 흐름을 자동으로 설치한다.
    public static class NpcRuntimeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -=
                HandleSceneLoaded;

            SceneManager.sceneLoaded +=
                HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene()
        {
            TryInstall();
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            TryInstall();
        }

        private static void TryInstall()
        {
            PlayerGridMovementController player =
                Object.FindFirstObjectByType<PlayerGridMovementController>();

            if (player == null)
            {
                return;
            }

            if (player.GetComponent<NpcRuntimeBootstrapController>() == null)
            {
                player.gameObject.AddComponent<NpcRuntimeBootstrapController>();
            }

            if (player.GetComponent<NpcInteractionController>() == null)
            {
                player.gameObject.AddComponent<NpcInteractionController>();
            }
        }
    }
}
