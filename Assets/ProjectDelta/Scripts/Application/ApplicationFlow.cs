using ProjectDelta.Infrastructure;

namespace ProjectDelta.Application
{
    public sealed class ApplicationFlow
    {
        private readonly ISceneLoaderService _sceneLoader;
        private readonly ILogService _log;

        public ApplicationFlow(ISceneLoaderService sceneLoader, ILogService log)
        {
            _sceneLoader = sceneLoader;
            _log = log;
        }

        public void EnterTitle()
        {
            _log.Info("Entering TitleScene");
            _sceneLoader.LoadSingle(SceneNames.Title);
        }
    }
}
