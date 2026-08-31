using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectDelta.Presentation
{
    // 117일차: NpcRuntimeInstaller(113일차)와 같은 방식으로, 씬을 직접 수정하지 않아도
    // DungeonScene의 Player에 EventBattleController를 자동으로 설치한다.
    public static class EventBattleRuntimeInstaller
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

            if (player.GetComponent<EventBattleController>() == null)
            {
                player.gameObject.AddComponent<EventBattleController>();
            }
        }
    }
}
