using System;
using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Data;
using UnityEngine.InputSystem;

namespace ProjectDelta.Infrastructure
{
    // Only one action map is enabled at a time so exploration and battle
    // input can never be active together (see doc 10.1 입력 액션 맵).
    public sealed class InputService : IInputService
    {
        private readonly InputActionAsset _actions;
        private InputActionMap _activeMap;

        // 137일차: 리매핑 진행 중인 작업 - 새 요청이 오면 이전 것부터 취소한다.
        private InputActionRebindingExtensions.RebindingOperation _activeRebind;

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

        public string GetBindingDisplayString(
            string mapName,
            string actionName)
        {
            InputAction action =
                FindAction(mapName, actionName);

            return action != null
                ? action.GetBindingDisplayString(0)
                : string.Empty;
        }

        public void StartRebind(
            string mapName,
            string actionName,
            Action<string> onCompleted,
            Action onCanceled)
        {
            InputAction action =
                FindAction(mapName, actionName);

            if (action == null)
            {
                onCanceled?.Invoke();
                return;
            }

            _activeRebind?.Cancel();

            // 마우스 포인터 위치/이동처럼 매 프레임 값이 바뀌는 잡음성 컨트롤은 제외해서
            // "다음에 누르는 실제 키/버튼"만 새 바인딩으로 잡히게 한다.
            _activeRebind =
                action.PerformInteractiveRebinding(0)
                    .WithControlsExcluding("Mouse/position")
                    .WithControlsExcluding("Mouse/delta")
                    .OnMatchWaitForAnother(0.1f)
                    .OnComplete(operation =>
                    {
                        string overridePath =
                            action.bindings[0].effectivePath;

                        operation.Dispose();
                        _activeRebind = null;

                        onCompleted?.Invoke(overridePath);
                    })
                    .OnCancel(operation =>
                    {
                        operation.Dispose();
                        _activeRebind = null;

                        onCanceled?.Invoke();
                    })
                    .Start();
        }

        public void ApplyBindingOverride(
            string mapName,
            string actionName,
            string overridePath)
        {
            InputAction action =
                FindAction(mapName, actionName);

            if (action == null
                || string.IsNullOrEmpty(overridePath))
            {
                return;
            }

            action.ApplyBindingOverride(
                0,
                overridePath);
        }

        public void ApplyBindingOverrides(
            IEnumerable<KeyBindingEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (KeyBindingEntry entry in entries)
            {
                if (entry == null
                    || string.IsNullOrEmpty(entry.ActionId)
                    || string.IsNullOrEmpty(entry.KeyboardBinding))
                {
                    continue;
                }

                // ActionId는 "MapName/ActionName" 형식으로 저장한다.
                string[] parts =
                    entry.ActionId.Split('/');

                if (parts.Length != 2)
                {
                    continue;
                }

                ApplyBindingOverride(
                    parts[0],
                    parts[1],
                    entry.KeyboardBinding);
            }
        }

        private InputAction FindAction(
            string mapName,
            string actionName)
        {
            InputActionMap map =
                _actions.FindActionMap(mapName, throwIfNotFound: false);

            return map?.FindAction(actionName);
        }
    }
}
