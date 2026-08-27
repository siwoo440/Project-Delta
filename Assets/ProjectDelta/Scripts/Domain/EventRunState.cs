using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 107일차: 이벤트 선택지 조건(Flag 종류)과 이후 이벤트 저장·기록(112일차 예정)이
    // 함께 쓸 플래그 저장소. 지금은 boolean 플래그 조회/설정만 담당한다.
    public sealed class EventRunState
    {
        private readonly HashSet<string> flags =
            new HashSet<string>();

        public bool HasFlag(
            string flagName)
        {
            return !string.IsNullOrEmpty(
                    flagName)
                && flags.Contains(
                    flagName);
        }

        public void SetFlag(
            string flagName,
            bool value)
        {
            if (string.IsNullOrEmpty(
                    flagName))
            {
                return;
            }

            if (value)
            {
                flags.Add(
                    flagName);
            }
            else
            {
                flags.Remove(
                    flagName);
            }
        }

        // 세이브/로드 복원용.
        public void RestoreFrom(
            IEnumerable<string> restoredFlags)
        {
            flags.Clear();

            if (restoredFlags == null)
            {
                return;
            }

            foreach (string flagName in restoredFlags)
            {
                if (!string.IsNullOrEmpty(
                        flagName))
                {
                    flags.Add(
                        flagName);
                }
            }
        }

        public IReadOnlyCollection<string> Flags =>
            flags;
    }
}
