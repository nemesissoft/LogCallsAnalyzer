//DO_NOT_ANALYZE_BY: LogCallsAnalyzer
using System;
using Serilog;
using Serilog.Events;

namespace LoggingAbstractions.Serilog
{
    public class Log : ILog
    {
        private readonly ILogger _log;

        public Log() => _log = new NullLogger();
        internal Log(ILogger log)
        {
            _log = log;
            IsDebugEnabled = _log.IsEnabled(LogEventLevel.Debug);
            IsInfoEnabled = _log.IsEnabled(LogEventLevel.Information);
            IsWarnEnabled = _log.IsEnabled(LogEventLevel.Warning);
            IsErrorEnabled = _log.IsEnabled(LogEventLevel.Error);
            IsFatalEnabled = _log.IsEnabled(LogEventLevel.Fatal);
        }

         void ILog.Debug(object message) => _log.Debug(message.ToString() ?? "");
         void ILog.Debug(object message, Exception ex) => _log.Debug(ex, message.ToString() ?? "");
         void ILog.DebugFormat(string format, params object?[] args) => _log.Debug(format, args);
         void ILog.DebugFormat(string format, object? arg0) => _log.Debug<object?>(format, arg0);
         void ILog.DebugFormat(string format, object? arg0, object? arg1) => _log.Debug<object?, object?>(format, arg0, arg1);
         void ILog.DebugFormat(string format, object? arg0, object? arg1, object? arg2) => _log.Debug<object?, object?, object?>(format, arg0, arg1, arg2);
         void ILog.DebugFormat(IFormatProvider provider, string format, params object?[] args) => _log.Debug(format, args);
              
         void ILog.Info(object message) => _log.Information(message.ToString() ?? "");
         void ILog.Info(object message, Exception ex) => _log.Information(ex, message.ToString() ?? "");
         void ILog.InfoFormat(string format, params object?[] args) => _log.Information(format, args);
         void ILog.InfoFormat(string format, object? arg0) => _log.Information<object?>(format, arg0);
         void ILog.InfoFormat(string format, object? arg0, object? arg1) => _log.Information<object?, object?>(format, arg0, arg1);
         void ILog.InfoFormat(string format, object? arg0, object? arg1, object? arg2) => _log.Information<object?, object?, object?>(format, arg0, arg1, arg2);
         void ILog.InfoFormat(IFormatProvider provider, string format, params object?[] args) => _log.Information(format, args);
              
         void ILog.Warn(object message) => _log.Warning(message.ToString() ?? "");
         void ILog.Warn(object message, Exception ex) => _log.Warning(ex, message.ToString() ?? "");
         void ILog.WarnFormat(string format, params object?[] args) => _log.Warning(format, args);
         void ILog.WarnFormat(string format, object? arg0) => _log.Warning<object?>(format, arg0);
         void ILog.WarnFormat(string format, object? arg0, object? arg1) => _log.Warning<object?, object?>(format, arg0, arg1);
         void ILog.WarnFormat(string format, object? arg0, object? arg1, object? arg2) => _log.Warning<object?, object?, object?>(format, arg0, arg1, arg2);
         void ILog.WarnFormat(IFormatProvider provider, string format, params object?[] args) => _log.Warning(format, args);
              
         void ILog.Error(object message) => _log.Error(message.ToString() ?? "");
         void ILog.Error(object message, Exception ex) => _log.Error(ex, message.ToString() ?? "");
         void ILog.ErrorFormat(string format, params object?[] args) => _log.Error(format, args);
         void ILog.ErrorFormat(string format, object? arg0) => _log.Error<object?>(format, arg0);
         void ILog.ErrorFormat(string format, object? arg0, object? arg1) => _log.Error<object?, object?>(format, arg0, arg1);
         void ILog.ErrorFormat(string format, object? arg0, object? arg1, object? arg2) => _log.Error<object?, object?, object?>(format, arg0, arg1, arg2);
         void ILog.ErrorFormat(IFormatProvider provider, string format, params object?[] args) => _log.Error(format, args);
              
         void ILog.Fatal(object message) => _log.Fatal(message.ToString() ?? "");
         void ILog.Fatal(object message, Exception ex) => _log.Fatal(ex, message.ToString() ?? "");
         void ILog.FatalFormat(string format, params object?[] args) => _log.Fatal(format, args);
         void ILog.FatalFormat(string format, object? arg0) => _log.Fatal<object?>(format, arg0);
         void ILog.FatalFormat(string format, object? arg0, object? arg1) => _log.Fatal<object?, object?>(format, arg0, arg1);
         void ILog.FatalFormat(string format, object? arg0, object? arg1, object? arg2) => _log.Fatal<object?, object?, object?>(format, arg0, arg1, arg2);
         void ILog.FatalFormat(IFormatProvider provider, string format, params object?[] args) => _log.Fatal(format, args);

        public bool IsDebugEnabled { get; }
        public bool IsInfoEnabled { get; }
        public bool IsWarnEnabled { get; }
        public bool IsErrorEnabled { get; }
        public bool IsFatalEnabled { get; }
    }
}
