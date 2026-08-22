namespace ProjectDelta.Infrastructure
{
    public interface ILogService
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}
