using UnityEngine.InputSystem;

namespace ProjectDelta.Infrastructure
{
    // Only one action map is enabled at a time so exploration and battle
    // input can never be active together (see doc 10.1 입력 액션 맵).
    public sealed class InputService : IInputService
    {
        private readonly InputActionAsset _actions;
        private InputActionMap _activeMap;

        public InputService(InputActionAsset actions)
        {
            _actions = actions;
        }

        public void SetActiveMap(string mapName)
        {
            _activeMap?.Disable();

            _activeMap = _actions.FindActionMap(mapName, throwIfNotFound: true);
            _activeMap.Enable();
        }
    }
}
