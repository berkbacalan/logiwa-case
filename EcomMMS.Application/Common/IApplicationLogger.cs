namespace EcomMMS.Application.Common
{
    public interface IApplicationLogger
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogCacheError(Exception exception, string operation, string key);
        void LogDatabaseError(Exception exception, string operation);
        void LogValidationError(string operation, string details);
        void LogBusinessLogicError(string operation, string details);
    }
} 