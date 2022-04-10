namespace LogCallsAnalyzer.Parser
{
    /// <summary>An element parsed from a message template string.</summary>
    abstract class MessageTemplateToken
    {
        public int StartIndex { get; }
        public int Length { get; }

        protected MessageTemplateToken(int startIndex, int length)
        {
            StartIndex = startIndex;
            Length = length;
        }

        public MessageTemplateDiagnostic ToDiagnostic(string diagnostics, bool mustBeRemapped = true)
            => new(StartIndex, Length, diagnostics, mustBeRemapped);
    }
}