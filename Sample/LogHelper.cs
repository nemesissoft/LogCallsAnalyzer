//DO_NOT_ANALYZE_BY: LogCallsAnalyzer
using LoggingAbstractions;

namespace Sample
{
    [JetBrains.Annotations.PublicAPI]
    public static class LogHelper
    {
        [Serilog.Core.MessageTemplateFormatMethod("message")] public static void Debug(this ILog log, IClientRequestInfo? request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void DebugFormat(this ILog log, IClientRequestInfo? request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void DebugFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void DebugFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void DebugFormat(this ILog log, IClientRequestInfo? request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod("message")] public static void Info(this ILog log, IClientRequestInfo? request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void InfoFormat(this ILog log, IClientRequestInfo? request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void InfoFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void InfoFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void InfoFormat(this ILog log, IClientRequestInfo? request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod("message")] public static void Warn(this ILog log, IClientRequestInfo? request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void WarnFormat(this ILog log, IClientRequestInfo? request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void WarnFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void WarnFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void WarnFormat(this ILog log, IClientRequestInfo? request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod("message")] public static void Error(this ILog log, IClientRequestInfo? request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void ErrorFormat(this ILog log, IClientRequestInfo? request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void ErrorFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void ErrorFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void ErrorFormat(this ILog log, IClientRequestInfo? request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod("message")] public static void Fatal(this ILog log, IClientRequestInfo? request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void FatalFormat(this ILog log, IClientRequestInfo? request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void FatalFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void FatalFormat(this ILog log, IClientRequestInfo? request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod("format")] public static void FatalFormat(this ILog log, IClientRequestInfo? request, string format, params object[] args) { }
    }

    [JetBrains.Annotations.PublicAPI]
    public interface IClientRequestInfo
    {
        string ClientRequestId { get; }
    }
}
