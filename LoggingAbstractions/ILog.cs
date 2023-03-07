using System;
using Serilog.Core;

namespace LoggingAbstractions
{
    public interface ILog
    {
        [MessageTemplateFormatMethod("message")] void Debug(object message);
        [MessageTemplateFormatMethod("message")] void Debug(object message, Exception ex);
        [MessageTemplateFormatMethod("format")] void DebugFormat(string format, params object?[] args);
        [MessageTemplateFormatMethod("format")] void DebugFormat(string format, object? arg0);
        [MessageTemplateFormatMethod("format")] void DebugFormat(string format, object? arg0, object? arg1);
        [MessageTemplateFormatMethod("format")] void DebugFormat(string format, object? arg0, object? arg1, object? arg2);
        [MessageTemplateFormatMethod("format")] void DebugFormat(IFormatProvider provider, string format, params object?[] args);

        [MessageTemplateFormatMethod("message")] void Info(object message);
        [MessageTemplateFormatMethod("message")] void Info(object message, Exception ex);
        [MessageTemplateFormatMethod("format")] void InfoFormat(string format, params object?[] args);
        [MessageTemplateFormatMethod("format")] void InfoFormat(string format, object? arg0);
        [MessageTemplateFormatMethod("format")] void InfoFormat(string format, object? arg0, object? arg1);
        [MessageTemplateFormatMethod("format")] void InfoFormat(string format, object? arg0, object? arg1, object? arg2);
        [MessageTemplateFormatMethod("format")] void InfoFormat(IFormatProvider provider, string format, params object?[] args);

        [MessageTemplateFormatMethod("message")] void Warn(object message);
        [MessageTemplateFormatMethod("message")] void Warn(object message, Exception ex);
        [MessageTemplateFormatMethod("format")] void WarnFormat(string format, params object?[] args);
        [MessageTemplateFormatMethod("format")] void WarnFormat(string format, object? arg0);
        [MessageTemplateFormatMethod("format")] void WarnFormat(string format, object? arg0, object? arg1);
        [MessageTemplateFormatMethod("format")] void WarnFormat(string format, object? arg0, object? arg1, object? arg2);
        [MessageTemplateFormatMethod("format")] void WarnFormat(IFormatProvider provider, string format, params object?[] args);

        [MessageTemplateFormatMethod("message")] void Error(object message);
        [MessageTemplateFormatMethod("message")] void Error(object message, Exception ex);
        [MessageTemplateFormatMethod("format")] void ErrorFormat(string format, params object?[] args);
        [MessageTemplateFormatMethod("format")] void ErrorFormat(string format, object? arg0);
        [MessageTemplateFormatMethod("format")] void ErrorFormat(string format, object? arg0, object? arg1);
        [MessageTemplateFormatMethod("format")] void ErrorFormat(string format, object? arg0, object? arg1, object? arg2);
        [MessageTemplateFormatMethod("format")] void ErrorFormat(IFormatProvider provider, string format, params object?[] args);

        [MessageTemplateFormatMethod("message")] void Fatal(object message);
        [MessageTemplateFormatMethod("message")] void Fatal(object message, Exception ex);
        [MessageTemplateFormatMethod("format")] void FatalFormat(string format, params object?[] args);
        [MessageTemplateFormatMethod("format")] void FatalFormat(string format, object? arg0);
        [MessageTemplateFormatMethod("format")] void FatalFormat(string format, object? arg0, object? arg1);
        [MessageTemplateFormatMethod("format")] void FatalFormat(string format, object? arg0, object? arg1, object? arg2);
        [MessageTemplateFormatMethod("format")] void FatalFormat(IFormatProvider provider, string format, params object?[] args);



        bool IsDebugEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
    }
}
