using System;
namespace LogCallsAnalyzer.Tests.Helpers
{
    public static class SourceBuilder
    {
        public static readonly string NL = Environment.NewLine;
        
        public static readonly string IMPORTS = "using LoggingAbstractions; using LoggingAbstractions.Serilog;";
        public static readonly string CREATE_LOG = "ILog abstraction = new LoggingAbstractions.Serilog.Log();";

        public static string BuildTestSource(string testedMethodCall, string? additionalCode = null)
        {
            return @$"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            abstraction.{testedMethodCall};
        }}
    }}
    {additionalCode}
}}";
        }

        public static string BuildTestSourceExtension(string methodName, string arguments)
        {
            return @$"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            LogHelper.
{methodName}(abstraction, {arguments});
        }}
    }}

    {LOG_HELPER_SOURCE}
}}";
        }

        public const string LOG_HELPER_SOURCE = @"    
    public static class LogHelper
    {
        [Serilog.Core.MessageTemplateFormatMethod(""message"")] public static void Debug(this ILog log, IClientRequestInfo request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void DebugFormat(this ILog log, IClientRequestInfo request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void DebugFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void DebugFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void DebugFormat(this ILog log, IClientRequestInfo request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod(""message"")] public static void Info(this ILog log, IClientRequestInfo request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void InfoFormat(this ILog log, IClientRequestInfo request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void InfoFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void InfoFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void InfoFormat(this ILog log, IClientRequestInfo request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod(""message"")] public static void Warn(this ILog log, IClientRequestInfo request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void WarnFormat(this ILog log, IClientRequestInfo request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void WarnFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void WarnFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void WarnFormat(this ILog log, IClientRequestInfo request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod(""message"")] public static void Error(this ILog log, IClientRequestInfo request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void ErrorFormat(this ILog log, IClientRequestInfo request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void ErrorFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void ErrorFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void ErrorFormat(this ILog log, IClientRequestInfo request, string format, params object[] args) { }

        [Serilog.Core.MessageTemplateFormatMethod(""message"")] public static void Fatal(this ILog log, IClientRequestInfo request, string message) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void FatalFormat(this ILog log, IClientRequestInfo request, string format, object arg0) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void FatalFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void FatalFormat(this ILog log, IClientRequestInfo request, string format, object arg0, object arg1, object arg2) { }
        [Serilog.Core.MessageTemplateFormatMethod(""format"")] public static void FatalFormat(this ILog log, IClientRequestInfo request, string format, params object[] args) { }
    }
    
    public interface IClientRequestInfo
    {
        string ClientRequestId { get; }
    }";

    }
}
