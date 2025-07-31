using Microsoft.Extensions.Logging;

namespace EcomMMS.Application.Common
{
    public class ApplicationLogger : IApplicationLogger
    {
        private readonly ILogger<ApplicationLogger> _logger;

        public ApplicationLogger(ILogger<ApplicationLogger> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void LogCacheError(Exception exception, string operation, string key)
        {
            _logger.LogWarning(exception, "Cache operation failed - Operation: {Operation}, Key: {Key}", operation, key);
        }

        public void LogDatabaseError(Exception exception, string operation)
        {
            _logger.LogError(exception, "Database operation failed - Operation: {Operation}", operation);
        }

        public void LogValidationError(string operation, string details)
        {
            _logger.LogWarning("Validation failed - Operation: {Operation}, Details: {Details}", operation, details);
        }

        public void LogBusinessLogicError(string operation, string details)
        {
            _logger.LogWarning("Business logic error - Operation: {Operation}, Details: {Details}", operation, details);
        }
    }
} 