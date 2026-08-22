using UnityEngine;

namespace ProjectDelta.Infrastructure
{
    public sealed class LogService : ILogService
    {
        public void Info(string message) => Debug.Log($"[ProjectDelta] {message}");
        public void Warn(string message) => Debug.LogWarning($"[ProjectDelta] {message}");
        public void Error(string message) => Debug.LogError($"[ProjectDelta] {message}");
    }
}
