using System;
using System.Globalization;

namespace LogCallsAnalyzer.Parser
{
    /// <summary>A message template token representing a log event property.</summary>
    sealed class PropertyToken : MessageTemplateToken
    {
        private readonly int? _position;

        public string PropertyName { get; }
        public bool IsPositional => _position.HasValue;
        internal string RawText { get; }

        public PropertyToken(int startIndex, string propertyName, string rawText)
            : base(startIndex, rawText.Length)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));

            if (int.TryParse(PropertyName, NumberStyles.None, CultureInfo.InvariantCulture, out int position) && position >= 0)
                _position = position;
        }

        /// <summary>Try to get the integer value represented by the property name.</summary>
        /// <param name="position">The integer value, if present.</param>
        /// <returns>True if the property is positional, otherwise false.</returns>
        public bool TryGetPositionalValue(out int position)
        {
            if (_position == null)
            {
                position = 0;
                return false;
            }

            position = _position.Value;
            return true;
        }

        public override string ToString() => RawText;
    }
}
