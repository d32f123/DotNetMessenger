using System;
using System.Diagnostics;
using NLog;

namespace DotNetMessenger.Logger
{
    public class ChronoLogger : IDisposable
    {
        private const string TimerStartDefault = "TSTART: ";
        private const string TimerStopDefault = "TEND: ";

        private readonly Stopwatch _stopwatch;
        private readonly LogLevel _logLevel;
        private readonly string _formatString;
        private readonly object[] _formatValues;
        private readonly string _timerStopString;

        public ChronoLogger(string format, params object[] objects) : this(LogLevel.Info, format, objects) {}

        public ChronoLogger(LogLevel logLevel, string format, params object[] objects) 
            : this(TimerStartDefault, TimerStopDefault, logLevel, format, objects) {}

        public ChronoLogger(string tStart, string tEnd, LogLevel logLevel, string format, params object[] objects)
        {
            _timerStopString = tEnd;
            _formatString = format;
            _formatValues = objects;
            _logLevel = logLevel;
            NLogger.Logger.Log(_logLevel, tStart + format, objects);
            _stopwatch = new Stopwatch();
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public TimeSpan ElapsedTime => _stopwatch.Elapsed;

        public void Dispose()
        {
            _stopwatch.Stop();
            NLogger.Logger.Log(_logLevel, _timerStopString + _formatString, _formatValues);
            NLogger.Logger.Log(_logLevel, "Elapsed: {0}", _stopwatch.Elapsed);
        }
    }
}