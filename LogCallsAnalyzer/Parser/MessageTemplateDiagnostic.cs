namespace LogCallsAnalyzer.Parser
{
    internal sealed class MessageTemplateDiagnostic : MessageTemplateToken
    {
        public string? Diagnostic { get; }
        public bool MustBeRemapped { get; }

        public MessageTemplateDiagnostic(int startIndex, int length, string? diagnostic = null, bool mustBeRemapped = true)
            : base(startIndex, length)
        {
            Diagnostic = diagnostic;
            MustBeRemapped = mustBeRemapped;
        }
    }
}
