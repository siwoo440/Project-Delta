using System;

namespace ProjectDelta.Application
{
    public interface ISceneLoaderService
    {
        void LoadSingle(string sceneName, Action onComplete = null);
        void LoadAdditive(string sceneName, Action onComplete = null);
        void UnloadAdditive(string sceneName, Action onComplete = null);
    }
}
