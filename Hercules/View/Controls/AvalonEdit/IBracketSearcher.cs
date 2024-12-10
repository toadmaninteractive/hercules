﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using ICSharpCode.AvalonEdit.Document;

namespace Hercules.Controls.AvalonEdit
{
    /// <summary>
    /// Allows language specific search for matching brackets.
    /// </summary>
    public interface IBracketSearcher
    {
        /// <summary>
        /// Searches for a matching bracket from the given offset to the start of the document.
        /// </summary>
        /// <returns>A BracketSearchResult that contains the positions and lengths of the brackets. Return null if there is nothing to highlight.</returns>
        BracketSearchResult? SearchBracket(IDocument document, int offset);
    }

    public class DefaultBracketSearcher : IBracketSearcher
    {
        public static readonly DefaultBracketSearcher DefaultInstance = new DefaultBracketSearcher();

        public BracketSearchResult? SearchBracket(IDocument document, int offset)
        {
            return null;
        }
    }

    /// <summary>
    /// Describes a pair of matching brackets found by IBracketSearcher.
    /// </summary>
    public class BracketSearchResult
    {
        public int OpeningBracketOffset { get; }

        public int OpeningBracketLength { get; }

        public int ClosingBracketOffset { get; }

        public int ClosingBracketLength { get; }

        public int DefinitionHeaderOffset { get; set; }

        public int DefinitionHeaderLength { get; set; }

        public BracketSearchResult(int openingBracketOffset, int openingBracketLength,
                                   int closingBracketOffset, int closingBracketLength)
        {
            this.OpeningBracketOffset = openingBracketOffset;
            this.OpeningBracketLength = openingBracketLength;
            this.ClosingBracketOffset = closingBracketOffset;
            this.ClosingBracketLength = closingBracketLength;
        }
    }
}
