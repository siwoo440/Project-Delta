using System;

namespace ProjectDelta.Application
{
    public static class AutoSaveNotification
    {
        public static event Action Saved;

        public static void RaiseSaved()
        {
            Saved?.Invoke();
        }
    }
}
