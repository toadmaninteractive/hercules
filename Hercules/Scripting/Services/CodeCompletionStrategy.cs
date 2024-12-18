using Acornima;
using Hercules.Documents;
using Hercules.Forms.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Hercules.Scripting
{
    public interface ICodeCompletionStrategy
    {
        bool SuggestCompletion(TextDocument textDocument, int caretOffset, CompletionDataList result, [MaybeNullWhen(false)] out string prefix);
    }

    public class CodeCompletionStrategy : ICodeCompletionStrategy
    {
        private readonly IReadOnlyList<CompletionData> fields;
        private readonly IReadOnlyList<CompletionData> enums;
        private readonly IReadOnlyList<IDocument> documents;

        private static readonly Acornima.Parser parser = new Acornima.Parser(new ParserOptions { Tolerant = true });

        public CodeCompletionStrategy(IReadOnlyList<IDocument> documents, FormSchema? schema)
        {
            this.documents = documents;
            if (schema == null)
            {
                fields = Array.Empty<CompletionData>();
                enums = Array.Empty<CompletionData>();
            }
            else
            {
                var recordFields = schema.Structs.Values.SelectMany(f => f.Fields);
                fields = recordFields.Select(f => f.Name).Concat(new[] { "_id" }).Distinct().Select(f => new CompletionData(f, "Field")).ToList();
                enums = schema.Enums.Values.SelectMany(f => f.Values.Select(v => new CompletionData(v, "Enumeration"))).ToList();
            }
        }

        private static string? GetTextBeforeInput(TextDocument textDocument, int caretOffset)
        {
            var charOffset = textDocument.GetOffset(textDocument.GetLocation(caretOffset));
            var lineOffset = textDocument.GetLineByOffset(charOffset).Offset;
            string text = textDocument.GetText(lineOffset, charOffset - lineOffset);

            return text;
        }

        private bool Tokenize(string code, out IReadOnlyList<Token> tokens)
        {
            var scanner = new Acornima.Tokenizer(code, new TokenizerOptions
            {
                Tolerant = true,
            });

            var result = new List<Token>();
            tokens = result;
            try
            {
                scanner.Next();
                Token token = scanner.Current;
                while (token.Kind != TokenKind.EOF)
                {
                    result.Add(token);
                    scanner.Next();
                    token = scanner.Current;
                }
                return result.Count > 0;
            }
            catch (Acornima.ParseErrorException)
            {
                return false;
            }
        }

        private Acornima.Ast.Script? cachedScript;

        private enum CompletionContextKind
        {
            Unknown,
            Variable,
            Property,
            String,
        }

        private static bool IsDot(in Token token)
        {
            return token.Kind == TokenKind.Punctuator && token.Value is ".";
        }

        public bool SuggestCompletion(TextDocument textDocument, int caretOffset, CompletionDataList result, [MaybeNullWhen(false)] out string prefix)
        {
            prefix = default!;

            try
            {
                cachedScript = parser.ParseScript(textDocument.Text);
            }
            catch (Acornima.ParseErrorException)
            {
            }

            var line = GetTextBeforeInput(textDocument, caretOffset);
            if (line == null)
                return false;
            line = line.TrimStart();

            string before = line;
            prefix = "";
            CompletionContextKind kind = CompletionContextKind.Unknown;
            if (Tokenize(line, out var tokens))
            {
                // foreach (var token in tokens)
                //     Logger.LogDebug(token.ToString());
                var lastToken = tokens[^1];
                if (lastToken.Kind == TokenKind.Identifier && lastToken.End == line.Length)
                {
                    // Potentially incomplete identifier
                    prefix = (string)lastToken.Value!;
                    before = line.Substring(0, tokens[^1].Start);
                    if (tokens.Count > 1 && IsDot(tokens[^2]))
                        kind = CompletionContextKind.Property;
                    else
                        kind = CompletionContextKind.Variable;
                }
                else if (IsDot(lastToken))
                {
                    kind = CompletionContextKind.Property;
                }
                else
                {
                    kind = CompletionContextKind.Variable;
                }
            }
            else
            {
                var sepIndex = line.LastIndexOfAny(new[] { '\"', '\'' });
                if (sepIndex >= 0)
                {
                    var quoteCount = line.Count(c => c == line[sepIndex]);
                    if (quoteCount % 2 == 1)
                    {
                        before = line.Substring(0, sepIndex + 1);
                        prefix = line.Substring(sepIndex + 1);
                        kind = CompletionContextKind.String;
                    }
                }
            }

            // Logger.LogDebug($"{kind} prefix:|{prefix}| before:|{before}|");

            var location = textDocument.GetLocation(caretOffset);
            var position = Position.From(location.Line, Math.Max(location.Column - 1, 0));

            switch (kind)
            {
                case CompletionContextKind.Property:
                    {
                        if (before.EndsWith("hercules.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesJsApi.Completion);
                        }
                        else if (before.EndsWith("hercules.db.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesDbApi.Completion);
                        }
                        else if (before.EndsWith("hercules.project.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesProjectApi.Completion);
                        }
                        else if (before.EndsWith("hercules.io.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesIOApi.Completion);
                        }
                        else if (before.EndsWith("hercules.xml.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesXmlApi.Completion);
                        }
                        else if (before.EndsWith("hercules.json.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesJsonApi.Completion);
                        }
                        else if (before.EndsWith("hercules.http.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesHttpApi.Completion);
                        }
                        else if (before.EndsWith("hercules.gapi.", StringComparison.Ordinal))
                        {
                            result.AddRange(HerculesGoogleApi.Completion);
                        }
                        else
                        {
                            VisitNode(cachedScript, result, position);
                            result.AddRange(fields);
                        }
                    }
                    break;

                case CompletionContextKind.Variable:
                    {
                        result.AddRange(fields);
                        result.Add(new CompletionData("hercules", "Hercules API"));
                        VisitNode(cachedScript, result, position);
                    }
                    break;

                case CompletionContextKind.String:
                    {
                        result.AddRange(enums);
                        result.AddRange(documents.Select(doc => new CompletionData(doc.DocumentId, "Document")));
                    }
                    break;
            }

            /*
            foreach (var c in result.Data)
            {
                Logger.LogDebug(c.Text);
            }*/

            result.Sort();
            return result.Data.Count > 0;
        }

        private void VisitNode(Acornima.Ast.Node? node, CompletionDataList result, Position position)
        {
            if (node == null)
                return;

            //if (position < node.Location.Start || position > node.Location.End)
            //    return;

            foreach (var n in node.ChildNodes)
            {
                VisitNode(n, result, position);
            }

            if (node is Acornima.Ast.Identifier id && !(position >= id.Location.Start && position <= id.Location.End))
            { 
                result.Add(new CompletionData(id.Name, ""));
            }
        }
    }
}
