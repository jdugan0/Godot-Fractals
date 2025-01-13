using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExpressionToGLSL
{
    public static class ExpressionParser
    {
        /// <summary>
        /// Main entry point: Convert a user-typed expression like "(1/z)^18 + z^3"
        /// into a GLSL snippet like:
        /// "complexAdd(
        ///      complex_pow_complex(complexDivide(vec2(1.0,0.0), z), vec2(18.0,0.0)), 
        ///      complex_pow_complex(z, vec2(3.0,0.0))
        /// )"
        /// </summary>
        public static string ConvertExpressionToGlsl(string expression)
        {
            // 1) Tokenize the input
            List<Token> tokens = Tokenize(expression);

            // 2) Parse into an abstract syntax tree (AST)
            Parser parser = new Parser(tokens);
            AstNode ast = parser.ParseExpression();

            // 3) Convert the AST to GLSL code
            return ast.ToGlsl();
        }

        #region Tokenization

        private enum TokenType
        {
            Number,   // e.g. 123, 3.14
            Z,        // 'z'
            Plus,     // '+'
            Minus,    // '-'
            Asterisk, // '*'
            Slash,    // '/'
            Caret,    // '^'
            LParen,   // '('
            RParen,   // ')'
            EOF
        }

        private class Token
        {
            public TokenType Type;
            public string Text;

            public Token(TokenType type, string text)
            {
                Type = type;
                Text = text;
            }

            public override string ToString() => $"{Type}('{Text}')";
        }

        /// <summary>
        /// Convert a raw string into a list of tokens
        /// (e.g. "(1/z)^18 + z^3" => LParen, Number("1"), Slash, Z, RParen, Caret, Number("18"), Plus, Z, Caret, Number("3"))
        /// </summary>
        private static List<Token> Tokenize(string input)
        {
            List<Token> tokens = new List<Token>();
            int pos = 0;
            while (pos < input.Length)
            {
                char c = input[pos];

                if (char.IsWhiteSpace(c))
                {
                    // skip whitespace
                    pos++;
                }
                else if (c == '(')
                {
                    tokens.Add(new Token(TokenType.LParen, c.ToString()));
                    pos++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenType.RParen, c.ToString()));
                    pos++;
                }
                else if (c == '+')
                {
                    tokens.Add(new Token(TokenType.Plus, c.ToString()));
                    pos++;
                }
                else if (c == '-')
                {
                    tokens.Add(new Token(TokenType.Minus, c.ToString()));
                    pos++;
                }
                else if (c == '*')
                {
                    tokens.Add(new Token(TokenType.Asterisk, c.ToString()));
                    pos++;
                }
                else if (c == '/')
                {
                    tokens.Add(new Token(TokenType.Slash, c.ToString()));
                    pos++;
                }
                else if (c == '^')
                {
                    tokens.Add(new Token(TokenType.Caret, c.ToString()));
                    pos++;
                }
                else if (c == 'z' || c == 'Z')
                {
                    // We treat 'z' or 'Z' as the same variable
                    tokens.Add(new Token(TokenType.Z, "z"));
                    pos++;
                }
                else if (char.IsDigit(c) || c == '.')
                {
                    // parse a number
                    int startPos = pos;
                    bool hasDot = (c == '.');
                    pos++;
                    while (pos < input.Length)
                    {
                        char nc = input[pos];
                        if (char.IsDigit(nc))
                        {
                            pos++;
                        }
                        else if (nc == '.' && !hasDot)
                        {
                            // only allow one dot
                            hasDot = true;
                            pos++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    string numberText = input.Substring(startPos, pos - startPos);
                    tokens.Add(new Token(TokenType.Number, numberText));
                }
                else
                {
                    throw new Exception($"Unexpected character '{c}' at position {pos}");
                }
            }

            tokens.Add(new Token(TokenType.EOF, ""));
            return tokens;
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Grammar (simplified):
        /// Expression -> Term (( "+" | "-" ) Term)* 
        /// Term       -> Factor (( "*" | "/" ) Factor)*
        /// Factor     -> Base ("^" Factor)?
        /// Base       -> Number | 'z' | "(" Expression ")"
        /// 
        /// Where 'z' is treated as a complex variable, 
        /// and Number is a real (converted to e.g. vec2(N,0)).
        /// 
        /// We do left-associative parsing for +, -, *, /, and ^ for simplicity.
        /// </summary>
        private class Parser
        {
            private readonly List<Token> _tokens;
            private int _pos;

            public Parser(List<Token> tokens)
            {
                _tokens = tokens;
                _pos = 0;
            }

            private Token Current => _tokens[_pos];

            private Token Eat(TokenType type)
            {
                if (Current.Type == type)
                {
                    Token t = Current;
                    _pos++;
                    return t;
                }
                else
                {
                    throw new Exception($"Expected token {type}, got {Current.Type} at position {_pos}");
                }
            }

            private bool Match(params TokenType[] types)
            {
                foreach (var t in types)
                {
                    if (Current.Type == t) return true;
                }
                return false;
            }

            /// <summary>
            /// Parse an Expression
            /// </summary>
            public AstNode ParseExpression()
            {
                // Expression -> Term (( "+" | "-" ) Term)* 
                AstNode left = ParseTerm();

                while (Match(TokenType.Plus, TokenType.Minus))
                {
                    Token op = Current;
                    _pos++;
                    AstNode right = ParseTerm();
                    if (op.Type == TokenType.Plus)
                        left = new AstBinOp(left, right, BinOpType.Add);
                    else
                        left = new AstBinOp(left, right, BinOpType.Subtract);
                }

                return left;
            }

            /// <summary>
            /// Parse a Term
            /// </summary>
            private AstNode ParseTerm()
            {
                // Term -> Factor (( "*" | "/" ) Factor)*
                AstNode left = ParseFactor();

                while (Match(TokenType.Asterisk, TokenType.Slash))
                {
                    Token op = Current;
                    _pos++;
                    AstNode right = ParseFactor();
                    if (op.Type == TokenType.Asterisk)
                        left = new AstBinOp(left, right, BinOpType.Multiply);
                    else
                        left = new AstBinOp(left, right, BinOpType.Divide);
                }

                return left;
            }

            /// <summary>
            /// Parse a Factor
            /// </summary>
            private AstNode ParseFactor()
            {
                // Factor -> Base ("^" Factor)?
                // We parse exponent as right-associative (but here we'll keep it left for simplicity, or do a small recursion).
                AstNode baseNode = ParseBase();

                while (Match(TokenType.Caret))
                {
                    // '^'
                    Token op = Current;
                    _pos++;
                    AstNode exponent = ParseFactor(); // read exponent as another factor
                    baseNode = new AstBinOp(baseNode, exponent, BinOpType.Power);
                }

                return baseNode;
            }

            /// <summary>
            /// Parse a Base
            /// </summary>
            private AstNode ParseBase()
            {
                // Base -> Number | 'z' | "(" Expression ")"
                if (Match(TokenType.LParen))
                {
                    Eat(TokenType.LParen);
                    AstNode expr = ParseExpression();
                    Eat(TokenType.RParen);
                    return expr;
                }
                else if (Match(TokenType.Number))
                {
                    Token t = Eat(TokenType.Number);
                    return new AstNumber(t.Text);
                }
                else if (Match(TokenType.Z))
                {
                    Eat(TokenType.Z);
                    return new AstVariableZ();
                }
                else
                {
                    throw new Exception($"Unexpected token {Current.Type} when expecting a Base");
                }
            }
        }

        #endregion

        #region AST

        /// <summary>
        /// Abstract Syntax Tree node
        /// </summary>
        private abstract class AstNode
        {
            /// <summary>
            /// Convert the AST node to a snippet of GLSL code (string).
            /// We'll rely on the user having defined:
            ///     vec2 complexAdd(vec2 a, vec2 b);
            ///     vec2 complexSub(vec2 a, vec2 b);
            ///     vec2 complexMultiply(vec2 a, vec2 b);
            ///     vec2 complexDivide(vec2 a, vec2 b);
            ///     vec2 complex_pow_complex(vec2 base, vec2 exp);
            /// for the shader.
            /// </summary>
            public abstract string ToGlsl();
        }

        private class AstNumber : AstNode
        {
            public double Value;

            public AstNumber(string text)
            {
                // Convert string to double
                // CultureInfo.InvariantCulture ensures '.' is decimal separator
                Value = double.Parse(text, CultureInfo.InvariantCulture); 
            }

            public override string ToGlsl()
            {
                return $"vec2({Value.ToString("G", CultureInfo.InvariantCulture)}, 0.0)";
            }
        }

        private class AstVariableZ : AstNode
        {
            public override string ToGlsl()
            {
                return "z";
            }
        }

        private enum BinOpType { Add, Subtract, Multiply, Divide, Power }

        private class AstBinOp : AstNode
        {
            public AstNode Left;
            public AstNode Right;
            public BinOpType Op;

            public AstBinOp(AstNode left, AstNode right, BinOpType op)
            {
                Left = left;
                Right = right;
                Op = op;
            }

            public override string ToGlsl()
            {
                string leftCode = Left.ToGlsl();
                string rightCode = Right.ToGlsl();

                switch (Op)
                {
                    case BinOpType.Add:
                        return $"complexAdd({leftCode}, {rightCode})";
                    case BinOpType.Subtract:
                        return $"complexSub({leftCode}, {rightCode})";
                    case BinOpType.Multiply:
                        return $"complexMult({leftCode}, {rightCode})";
                    case BinOpType.Divide:
                        return $"complexDivide({leftCode}, {rightCode})";
                    case BinOpType.Power:
                        return $"complex_pow_complex({leftCode}, {rightCode})";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion
    }
}
