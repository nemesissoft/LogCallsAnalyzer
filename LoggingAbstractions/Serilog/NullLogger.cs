using System;
using System.Collections.Generic;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace LoggingAbstractions.Serilog
{
    public class NullLogger : ILogger
    {
        ILogger ILogger.ForContext(ILogEventEnricher enricher) => this;

        ILogger ILogger.ForContext(IEnumerable<ILogEventEnricher> enrichers) => this;

        ILogger ILogger.ForContext(string propertyName, object value, bool destructureObjects) => this;

        ILogger ILogger.ForContext<TSource>() => this;

        ILogger ILogger.ForContext(Type source) => this;

        void ILogger.Write(LogEvent logEvent) { }
        void ILogger.Write(LogEventLevel level, string messageTemplate) { }
        void ILogger.Write<T>(LogEventLevel level, string messageTemplate, T propertyValue) { }
        void ILogger.Write<T0, T1>(LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Write<T0, T1, T2>(LogEventLevel level, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Write(LogEventLevel level, string messageTemplate, params object[] propertyValues) { }
        void ILogger.Write(LogEventLevel level, Exception exception, string messageTemplate) { }
        void ILogger.Write<T>(LogEventLevel level, Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Write<T0, T1>(LogEventLevel level, Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Write<T0, T1, T2>(LogEventLevel level, Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Write(LogEventLevel level, Exception exception, string messageTemplate, params object[] propertyValues) { }

        bool ILogger.IsEnabled(LogEventLevel level) => false;

        void ILogger.Verbose(string messageTemplate) { }
        void ILogger.Verbose<T>(string messageTemplate, T propertyValue) { }
        void ILogger.Verbose<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Verbose<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Verbose(string messageTemplate, params object[] propertyValues) { }
        void ILogger.Verbose(Exception exception, string messageTemplate) { }
        void ILogger.Verbose<T>(Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Verbose<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Verbose<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Verbose(Exception exception, string messageTemplate, params object[] propertyValues) { }

        void ILogger.Debug(string messageTemplate) { }
        void ILogger.Debug<T>(string messageTemplate, T propertyValue) { }
        void ILogger.Debug<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Debug<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Debug(string messageTemplate, params object[] propertyValues) { }
        void ILogger.Debug(Exception exception, string messageTemplate) { }
        void ILogger.Debug<T>(Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Debug<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Debug(Exception exception, string messageTemplate, params object[] propertyValues) { }

        void ILogger.Information(string messageTemplate) { }
        void ILogger.Information<T>(string messageTemplate, T propertyValue) { }
        void ILogger.Information<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Information<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Information(string messageTemplate, params object[] propertyValues) { }
        void ILogger.Information(Exception exception, string messageTemplate) { }
        void ILogger.Information<T>(Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Information<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Information(Exception exception, string messageTemplate, params object[] propertyValues) { }

        void ILogger.Warning(string messageTemplate) { }
        void ILogger.Warning<T>(string messageTemplate, T propertyValue) { }
        void ILogger.Warning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Warning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Warning(string messageTemplate, params object[] propertyValues) { }
        void ILogger.Warning(Exception exception, string messageTemplate) { }
        void ILogger.Warning<T>(Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Warning<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Warning(Exception exception, string messageTemplate, params object[] propertyValues) { }

        void ILogger.Error(string messageTemplate) { }
        void ILogger.Error<T>(string messageTemplate, T propertyValue) { }
        void ILogger.Error<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Error<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Error(string messageTemplate, params object[] propertyValues) { }
        void ILogger.Error(Exception exception, string messageTemplate) { }
        void ILogger.Error<T>(Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Error<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Error(Exception exception, string messageTemplate, params object[] propertyValues) { }

        void ILogger.Fatal(string messageTemplate) { }
        void ILogger.Fatal<T>(string messageTemplate, T propertyValue) { }
        void ILogger.Fatal<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Fatal<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Fatal(string messageTemplate, params object[] propertyValues) { }
        void ILogger.Fatal(Exception exception, string messageTemplate) { }
        void ILogger.Fatal<T>(Exception exception, string messageTemplate, T propertyValue) { }
        void ILogger.Fatal<T0, T1>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1) { }
        void ILogger.Fatal<T0, T1, T2>(Exception exception, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) { }
        void ILogger.Fatal(Exception exception, string messageTemplate, params object[] propertyValues) { }

        bool ILogger.BindMessageTemplate(string messageTemplate, object[] propertyValues, out MessageTemplate? parsedTemplate, out IEnumerable<LogEventProperty>? boundProperties)
        {
            parsedTemplate = null;
            boundProperties = null;
            return false;
        }

        bool ILogger.BindProperty(string propertyName, object value, bool destructureObjects, out LogEventProperty? property)
        {
            property = null;
            return false;
        }
    }
}
