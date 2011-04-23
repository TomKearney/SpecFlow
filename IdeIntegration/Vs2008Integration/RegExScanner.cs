using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Package;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow.Parser;

namespace TechTalk.SpecFlow.Vs2008Integration
{
    /// <summary>
    /// This class implements IScanner interface and performs
    /// text parsing on the base of rules' table. 
    /// </summary>
    internal class RegularExpressionScanner : IScanner
    {
        public RegularExpressionScanner(CultureInfo defaultLanguage)
        {
            var dialectServices = new GherkinDialectServices(defaultLanguage);
            gherkinDialect = dialectServices.GetDefaultDialect();
            patternTable =
                gherkinDialect.GetKeywords().Select(
                    keyword =>
                    keyword.Trim() == "*"
                        ? new RegularExpressionTableEntry("\\*", TokenColor.Keyword)
                        : new RegularExpressionTableEntry(keyword, TokenColor.Keyword)).ToArray();
        }

        private string sourceString;
        private int currentPos;

        private readonly RegularExpressionTableEntry[] patternTable;
        private readonly GherkinDialect gherkinDialect;

        /// <summary>
        /// This method is used to compare initial string with regular expression patterns from 
        /// correspondence table
        /// </summary>
        /// <param name="source">Initial string to parse</param>
        /// <param name="charsMatched">This parameter is used to get the size of matched block</param>
        /// <param name="color">Color of matched block</param>
        private void MatchRegEx(string source, ref int charsMatched, ref TokenColor color)
        {
            foreach (RegularExpressionTableEntry tableEntry in patternTable)
            {
                bool badPattern = false;
                Regex expr = null;

                try
                {
                    // Create Regex instance using pattern from current element of associations table
                    expr = new Regex("^" + tableEntry.Pattern);
                }
                catch (ArgumentException)
                {
                    badPattern = true;
                }

                if (badPattern)
                {
                    continue;
                }

                // Searching the source string for an occurrence of the regular expression pattern
                // specified in the current element of correspondence table
                Match m = expr.Match(source);
                if (m.Success && m.Length != 0)
                {
                    charsMatched = m.Length;
                    color = tableEntry.Color;
                    return;
                }
            }

            // No matches found. So we return color scheme of usual text
            charsMatched = 1;
            color = TokenColor.Text;
        }

        /// <summary>
        /// This method is used to parse next language token from the current line and return information about it.
        /// </summary>
        /// <param name="tokenInfo"> The TokenInfo structure to be filled in.</param>
        /// <param name="state"> The scanner's current state value.</param>
        /// <returns>Returns true if a token was parsed from the current line and information returned;
        /// otherwise, returns false indicating no more tokens are on the current line.</returns>
        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
        {
            // If input string is empty - there is nothing to parse - so, return false
            if (sourceString.Length == 0)
            {
                return false;
            }

            TokenColor color = TokenColor.Text;
            int charsMatched = 0;

            // Compare input string with patterns from correspondence table
            MatchRegEx(sourceString, ref charsMatched, ref color);

            // Fill in TokenInfo structure on the basis of examination 
            if (tokenInfo != null)
            {
                tokenInfo.Color = color;
                tokenInfo.Type = TokenType.Text;
                tokenInfo.StartIndex = currentPos;
                tokenInfo.EndIndex = Math.Max(currentPos, currentPos + charsMatched - 1);
            }

            // Move current position
            currentPos += charsMatched;

            // Set an unprocessed part of string as a source
            sourceString = sourceString.Substring(charsMatched);

            return true;
        }

        /// <summary>
        /// This method is used to set the line to be parsed.
        /// </summary>
        /// <param name="source">The line to parse.</param>
        /// <param name="offset">The character offset in the line to start parsing from. 
        /// You have to pay attention to this value.</param>
        public void SetSource(string source, int offset)
        {
            sourceString = source;
            currentPos = offset;
        }

        /// <summary>
        /// Store information about patterns and colors of parsed text 
        /// </summary>
        private class RegularExpressionTableEntry
        {
            public readonly string Pattern;

            public readonly TokenColor Color;

            public RegularExpressionTableEntry(string pattern, TokenColor color)
            {
                Pattern = pattern;
                Color = color;
            }
        }
    }
}
