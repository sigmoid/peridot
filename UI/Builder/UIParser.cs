using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Peridot.UI.Builder
{
    /// <summary>
    /// Represents a parsed UI element with its type, attributes, and children
    /// </summary>
    public class UIElementNode
    {
        public string ElementType { get; set; } = "";
        public Dictionary<string, string> Attributes { get; set; } = new();
        public List<UIElementNode> Children { get; set; } = new();
        public string TextContent { get; set; } = "";
        public bool IsSelfClosing { get; set; } = false;
        public int LineNumber { get; set; } = 0;
        public int ColumnNumber { get; set; } = 0;
    }

    /// <summary>
    /// Tokenizer for UI markup syntax
    /// </summary>
    public class UITokenizer
    {
        public enum TokenType
        {
            OpenTag,        // <div
            CloseTag,       // </div>
            SelfCloseTag,   // />
            TagEnd,         // >
            AttributeName,  // bounds
            AttributeValue, // "0,0,100,100"
            Equals,         // =
            Text,           // text content
            Comment,        // <!-- comment -->
            EOF
        }

        public class Token
        {
            public TokenType Type { get; set; }
            public string Value { get; set; } = "";
            public int LineNumber { get; set; }
            public int ColumnNumber { get; set; }

            public override string ToString()
            {
                return $"{Type}: '{Value}' at {LineNumber}:{ColumnNumber}";
            }
        }

        private readonly string _input;
        private int _position;
        private int _line;
        private int _column;

        public UITokenizer(string input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _position = 0;
            _line = 1;
            _column = 1;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            
            while (_position < _input.Length)
            {
                SkipWhitespace();
                
                if (_position >= _input.Length)
                    break;

                var token = ReadNextToken();
                if (token != null)
                {
                    tokens.Add(token);
                }
            }

            tokens.Add(new Token { Type = TokenType.EOF, LineNumber = _line, ColumnNumber = _column });
            return tokens;
        }

        private Token ReadNextToken()
        {
            if (_position >= _input.Length)
                return null;

            char current = _input[_position];

            // Handle comments
            if (current == '<' && _position + 3 < _input.Length && 
                _input.Substring(_position, 4) == "<!--")
            {
                return ReadComment();
            }

            // Handle tags
            if (current == '<')
            {
                return ReadTag();
            }

            // Handle self-closing tag end
            if (current == '/' && _position + 1 < _input.Length && _input[_position + 1] == '>')
            {
                var token = new Token
                {
                    Type = TokenType.SelfCloseTag,
                    Value = "/>",
                    LineNumber = _line,
                    ColumnNumber = _column
                };
                Advance(2);
                return token;
            }

            // Handle tag end
            if (current == '>')
            {
                var token = new Token
                {
                    Type = TokenType.TagEnd,
                    Value = ">",
                    LineNumber = _line,
                    ColumnNumber = _column
                };
                Advance();
                return token;
            }

            // Handle attribute values (quoted strings)
            if (current == '"')
            {
                return ReadQuotedString();
            }

            // Handle equals sign in attributes
            if (current == '=')
            {
                var token = new Token
                {
                    Type = TokenType.Equals,
                    Value = "=",
                    LineNumber = _line,
                    ColumnNumber = _column
                };
                Advance();
                return token;
            }

            // Handle text content or attribute names
            if (char.IsLetter(current) || current == '_')
            {
                return ReadIdentifier();
            }

            // Handle text content between tags
            return ReadTextContent();
        }

        private Token ReadComment()
        {
            var startLine = _line;
            var startColumn = _column;
            var content = new StringBuilder();

            // Skip <!--
            Advance(4);

            while (_position < _input.Length)
            {
                if (_position + 2 < _input.Length && 
                    _input.Substring(_position, 3) == "-->")
                {
                    Advance(3);
                    break;
                }
                
                content.Append(_input[_position]);
                Advance();
            }

            return new Token
            {
                Type = TokenType.Comment,
                Value = content.ToString(),
                LineNumber = startLine,
                ColumnNumber = startColumn
            };
        }

        private Token ReadTag()
        {
            var startLine = _line;
            var startColumn = _column;
            var tagName = new StringBuilder();

            // Skip <
            Advance();

            // Check for close tag
            bool isCloseTag = false;
            if (_position < _input.Length && _input[_position] == '/')
            {
                isCloseTag = true;
                Advance();
            }

            // Read tag name
            while (_position < _input.Length && 
                   (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
            {
                tagName.Append(_input[_position]);
                Advance();
            }

            return new Token
            {
                Type = isCloseTag ? TokenType.CloseTag : TokenType.OpenTag,
                Value = tagName.ToString(),
                LineNumber = startLine,
                ColumnNumber = startColumn
            };
        }

        private Token ReadQuotedString()
        {
            var startLine = _line;
            var startColumn = _column;
            var content = new StringBuilder();

            // Skip opening quote
            Advance();

            while (_position < _input.Length && _input[_position] != '"')
            {
                if (_input[_position] == '\\' && _position + 1 < _input.Length)
                {
                    // Handle escaped characters
                    Advance();
                    if (_position < _input.Length)
                    {
                        char escaped = _input[_position];
                        content.Append(escaped switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'r' => '\r',
                            '\\' => '\\',
                            '"' => '"',
                            _ => escaped
                        });
                        Advance();
                    }
                }
                else
                {
                    content.Append(_input[_position]);
                    Advance();
                }
            }

            // Skip closing quote
            if (_position < _input.Length && _input[_position] == '"')
                Advance();

            return new Token
            {
                Type = TokenType.AttributeValue,
                Value = content.ToString(),
                LineNumber = startLine,
                ColumnNumber = startColumn
            };
        }

        private Token ReadIdentifier()
        {
            var startLine = _line;
            var startColumn = _column;
            var identifier = new StringBuilder();

            while (_position < _input.Length && 
                   (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_' || _input[_position] == '-'))
            {
                identifier.Append(_input[_position]);
                Advance();
            }

            return new Token
            {
                Type = TokenType.AttributeName,
                Value = identifier.ToString(),
                LineNumber = startLine,
                ColumnNumber = startColumn
            };
        }

        private Token ReadTextContent()
        {
            var startLine = _line;
            var startColumn = _column;
            var content = new StringBuilder();

            while (_position < _input.Length && _input[_position] != '<')
            {
                content.Append(_input[_position]);
                Advance();
            }

            var text = content.ToString().Trim();
            if (string.IsNullOrEmpty(text))
                return null;

            return new Token
            {
                Type = TokenType.Text,
                Value = text,
                LineNumber = startLine,
                ColumnNumber = startColumn
            };
        }

        private void SkipWhitespace()
        {
            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                Advance();
            }
        }

        private void Advance(int count = 1)
        {
            for (int i = 0; i < count && _position < _input.Length; i++)
            {
                if (_input[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }
    }

    /// <summary>
    /// Parser for UI markup syntax that creates a symbolic tree representation
    /// </summary>
    public class UIParser
    {
        private readonly List<UITokenizer.Token> _tokens;
        private int _position;

        public UIParser(List<UITokenizer.Token> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _position = 0;
        }

        public static UIElementNode ParseMarkup(string markup)
        {
            try
            {
                Console.WriteLine("UIParser: Starting markup parsing");
                var tokenizer = new UITokenizer(markup);
                var tokens = tokenizer.Tokenize();
                Console.WriteLine($"UIParser: Tokenized {tokens.Count} tokens");
                var parser = new UIParser(tokens);
                var result = parser.ParseDocument();
                Console.WriteLine("UIParser: Parsing completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UIParser: Error parsing markup: {ex.Message}");
                Console.WriteLine($"UIParser: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public UIElementNode ParseDocument()
        {
            var root = new UIElementNode
            {
                ElementType = "document",
                LineNumber = 1,
                ColumnNumber = 1
            };

            while (!IsAtEnd() && Current().Type != UITokenizer.TokenType.EOF)
            {
                var element = ParseElement();
                if (element != null)
                {
                    root.Children.Add(element);
                }
            }

            return root;
        }

        private UIElementNode ParseElement()
        {
            // Skip comments
            while (Current().Type == UITokenizer.TokenType.Comment)
            {
                Advance();
            }

            if (Current().Type != UITokenizer.TokenType.OpenTag)
            {
                var errorMsg = $"Expected opening tag at {Current().LineNumber}:{Current().ColumnNumber}, got {Current().Type}";
                Console.WriteLine($"UIParser Error: {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }

            var element = new UIElementNode
            {
                ElementType = Current().Value,
                LineNumber = Current().LineNumber,
                ColumnNumber = Current().ColumnNumber
            };

            Advance(); // consume opening tag

            // Parse attributes
            ParseAttributes(element);

            // Check if self-closing
            if (Current().Type == UITokenizer.TokenType.SelfCloseTag)
            {
                element.IsSelfClosing = true;
                Advance();
                return element;
            }

            // Expect tag end
            if (Current().Type != UITokenizer.TokenType.TagEnd)
            {
                var errorMsg = $"Expected '>' at {Current().LineNumber}:{Current().ColumnNumber}";
                Console.WriteLine($"UIParser Error: {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }
            Advance();

            // Parse children and text content
            ParseChildren(element);

            // Expect closing tag
            if (Current().Type != UITokenizer.TokenType.CloseTag || 
                Current().Value != element.ElementType)
            {
                var errorMsg = $"Expected closing tag for '{element.ElementType}' at {Current().LineNumber}:{Current().ColumnNumber}";
                Console.WriteLine($"UIParser Error: {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }
            Advance();

            // Expect final tag end
            if (Current().Type == UITokenizer.TokenType.TagEnd)
            {
                Advance();
            }

            return element;
        }

        private void ParseAttributes(UIElementNode element)
        {
            while (Current().Type == UITokenizer.TokenType.AttributeName)
            {
                var attributeName = Current().Value;
                Advance();

                // Expect = (we'll be lenient and skip it if not present)
                if (Current().Type == UITokenizer.TokenType.Equals)
                {
                    Advance();
                }

                // Expect attribute value
                if (Current().Type == UITokenizer.TokenType.AttributeValue)
                {
                    element.Attributes[attributeName] = Current().Value;
                    Advance();
                }
                else
                {
                    // Boolean attribute (no value)
                    element.Attributes[attributeName] = "true";
                }
            }
        }

        private void ParseChildren(UIElementNode element)
        {
            while (!IsAtEnd() && 
                   Current().Type != UITokenizer.TokenType.CloseTag && 
                   Current().Type != UITokenizer.TokenType.EOF)
            {
                if (Current().Type == UITokenizer.TokenType.OpenTag)
                {
                    var child = ParseElement();
                    if (child != null)
                    {
                        element.Children.Add(child);
                    }
                }
                else if (Current().Type == UITokenizer.TokenType.Text)
                {
                    element.TextContent += Current().Value;
                    Advance();
                }
                else if (Current().Type == UITokenizer.TokenType.Comment)
                {
                    Advance(); // Skip comments
                }
                else
                {
                    Advance(); // Skip unexpected tokens
                }
            }
        }

        private UITokenizer.Token Current()
        {
            if (_position >= _tokens.Count)
                return new UITokenizer.Token { Type = UITokenizer.TokenType.EOF };
            return _tokens[_position];
        }

        private void Advance()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count || Current().Type == UITokenizer.TokenType.EOF;
        }
    }

    /// <summary>
    /// Utility class for parsing common attribute values
    /// </summary>
    public static class AttributeParser
    {
        public static Rectangle ParseBounds(string boundsString)
        {
            try
            {
                if (string.IsNullOrEmpty(boundsString))
                    return Rectangle.Empty;

                var parts = boundsString.Split(',');
                if (parts.Length != 4)
                {
                    var errorMsg = $"Invalid bounds format: {boundsString}. Expected 'x,y,width,height'";
                    Console.WriteLine($"AttributeParser Error: {errorMsg}");
                    throw new ArgumentException(errorMsg);
                }

                return new Rectangle(
                    int.Parse(parts[0].Trim()),
                    int.Parse(parts[1].Trim()),
                    int.Parse(parts[2].Trim()),
                    int.Parse(parts[3].Trim())
                );
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                Console.WriteLine($"AttributeParser Error parsing bounds '{boundsString}': {ex.Message}");
                throw;
            }
        }

        public static Color ParseColor(string colorString)
        {
            try
            {
                if (string.IsNullOrEmpty(colorString))
                    return Color.Transparent;

                // Remove # if present
                if (colorString.StartsWith("#"))
                    colorString = colorString.Substring(1);

                // Handle 6-character hex (RGB)
                if (colorString.Length == 6)
                {
                    return new Color(
                        Convert.ToByte(colorString.Substring(0, 2), 16),
                        Convert.ToByte(colorString.Substring(2, 2), 16),
                        Convert.ToByte(colorString.Substring(4, 2), 16)
                    );
                }
                // Handle 8-character hex (ARGB)
                else if (colorString.Length == 8)
                {
                    return new Color(
                        Convert.ToByte(colorString.Substring(2, 2), 16),
                        Convert.ToByte(colorString.Substring(4, 2), 16),
                        Convert.ToByte(colorString.Substring(6, 2), 16),
                        Convert.ToByte(colorString.Substring(0, 2), 16)
                    );
                }

                var errorMsg = $"Invalid color format: {colorString}";
                Console.WriteLine($"AttributeParser Error: {errorMsg}");
                throw new ArgumentException(errorMsg);
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                Console.WriteLine($"AttributeParser Error parsing color '{colorString}': {ex.Message}");
                throw;
            }
        }

        public static bool ParseBool(string boolString)
        {
            if (string.IsNullOrEmpty(boolString))
                return false;

            switch (boolString.ToLower())
            {
                case "true":
                    return true;
                case "false":
                    return false;
                case "1":
                    return true;
                case "0":
                    return false;
                default:
                    var errorMsg = $"Invalid boolean format: {boolString}";
                    Console.WriteLine($"AttributeParser Error: {errorMsg}");
                    throw new ArgumentException(errorMsg);
            }
        }

        public static int ParseInt(string intString, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(intString))
                return defaultValue;

            return int.TryParse(intString, out var result) ? result : defaultValue;
        }

        public static float ParseFloat(string floatString, float defaultValue = 0f)
        {
            if (string.IsNullOrEmpty(floatString))
                return defaultValue;

            return float.TryParse(floatString, out var result) ? result : defaultValue;
        }
    }
}