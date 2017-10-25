using NLog;

namespace DotNetMessenger.Logger
{
    public static class NLogger
    {
        private static bool _isLoggerCreated = false;
        private static NLog.Logger _logger;

        public static NLog.Logger Logger
        {
            get
            {
                if (_isLoggerCreated)
                    return _logger;
                _logger = LogManager.GetCurrentClassLogger();
                return _logger;
            }
        }
    }
}