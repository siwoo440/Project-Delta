using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectDelta.Infrastructure
{
    public sealed class SceneLoaderService : MonoBehaviour, ISceneLoaderService
    {
        public void LoadSingle(string sceneName, Action onComplete = null)
        {
            StartCoroutine(LoadRoutine(sceneName, LoadSceneMode.Single, onComplete));
        }

        public void LoadAdditive(string sceneName, Action onComplete = null)
        {
            StartCoroutine(LoadRoutine(sceneName, LoadSceneMode.Additive, onComplete));
        }

        public void UnloadAdditive(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadRoutine(sceneName, onComplete));
        }

        private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode, Action onComplete)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator UnloadRoutine(string sceneName, Action onComplete)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
