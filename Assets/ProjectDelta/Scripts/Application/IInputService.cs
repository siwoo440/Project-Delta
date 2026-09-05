using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    public interface IInputService
    {
        void SetActiveMap(string mapName);

        // 137일차: 기획서 8.1절 "키보드/마우스/게임패드 리매핑" - 어떤 액션이든
        // mapName/actionName으로 지정해 재설정할 수 있게 한다.
        string GetBindingDisplayString(string mapName, string actionName);

        // 장치를 가리지 않고(키보드든 마우스든 게임패드든) 다음 입력을 그대로 새 바인딩으로
        // 받는다 - onCompleted에는 새로 적용된 바인딩 경로 문자열이 담겨 온다.
        void StartRebind(
            string mapName,
            string actionName,
            Action<string> onCompleted,
            Action onCanceled);

        void ApplyBindingOverride(
            string mapName,
            string actionName,
            string overridePath);

        void ApplyBindingOverrides(
            IEnumerable<KeyBindingEntry> entries);
    }
}
