using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace ExpressionToGLSL
{
    public static class ExpressionParser
    {
        /// <summary>
        /// Main entry point: Convert a user-typed expression like "(1/z)^18 + z^3"
        /// into a GLSL snippet such as:
        /// complexAdd(
        ///    complex_pow_complex(complexDivide(vec2(1.0, 0.0), z), vec2(18.0, 0.0)),
        ///    complex_pow_complex(z, vec2(3.0, 0.0))
        /// )
        /// 
        /// Also supports imaginary parts via 'i' and negative numbers like "-3.14" or "-2.5i".
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
            Number,     // e.g. "123", "3.14", or might be something with 'i' like "2.5i"
            Z,          // 'z' or 'Z'
            Plus,       // '+'
            Minus,      // '-'
            Asterisk,   // '*'
            Slash,      // '/'
            Caret,      // '^'
            LParen,     // '('
            RParen, // ')'
            Identifier,
            EndIdentifier,
            EOF
        }

        private class Token
        {
            public TokenType Type;
            public string Text;
            public bool IsImag;  // Whether this token is purely imaginary (e.g. "2.5i")

            public Token(TokenType type, string text, bool isImag = false)
            {
                Type = type;
                Text = text;
                IsImag = isImag;
            }

            public override string ToString() => $"{Type}('{Text}'){(IsImag ? "[i]" : "")}";
        }

        /// <summary>
        /// Convert a raw string into a list of tokens.
        /// For example "(1/z)^18 + z^3" => LParen, Number("1"), Slash, Z, RParen, Caret, Number("18"), Plus, Z, Caret, Number("3"), EOF
        /// Also supports something like "-3.14i" or "1.2i" or just "i" for 1i.
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
                    // Ignore whitespace
                    pos++;
                }
                else if (c == '(')
                {
                    tokens.Add(new Token(TokenType.LParen, "("));
                    pos++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenType.RParen, ")"));
                    if (pos < input.Length - 1)
                    {
                        if (input[pos + 1] == '(')
                        {
                            tokens.Add(new Token(TokenType.Asterisk, "*"));
                        }
                    }
                    pos++;
                }
                else if (c == '!')
                {
                    tokens.Add(new Token(TokenType.EndIdentifier, "!"));
                    pos++;
                }
                else if (c == '+')
                {
                    tokens.Add(new Token(TokenType.Plus, "+"));
                    pos++;
                }
                else if (c == '-')
                {
                    // Check for a "negative number" or just a minus operator
                    // We'll handle that more gracefully in the parser (unary minus or subtraction).
                    tokens.Add(new Token(TokenType.Minus, "-"));
                    pos++;
                }
                else if (c == '*')
                {
                    tokens.Add(new Token(TokenType.Asterisk, "*"));
                    pos++;
                }
                else if (c == '/')
                {
                    tokens.Add(new Token(TokenType.Slash, "/"));
                    pos++;
                }
                else if (c == '^')
                {
                    tokens.Add(new Token(TokenType.Caret, "^"));
                    pos++;
                }
                else if (c == 'z' || c == 'Z')
                {
                    tokens.Add(new Token(TokenType.Z, "z"));
                    pos++;
                }
                else if (char.IsDigit(c) || c == '.' || c == 'i')
                {
                    // Parse a numeric literal, possibly with an 'i' for imaginary.
                    ParseNumberOrImag(input, ref pos, tokens);
                }
                else if (char.IsLetter(c))
                {
                    if (input.Substring(pos).ToLower().StartsWith("ln"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "ln"));
                        pos += 2;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("sin"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "sin"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("cos"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "cos"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("tan"))
                    {
                        tokens.Add(new Token(TokenType.Identifier, "tan"));
                        pos += 3;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("e"))
                    {
                        tokens.Add(new Token(TokenType.Number, Mathf.E.ToString()));
                        pos += 1;
                    }
                    else if (input.Substring(pos).ToLower().StartsWith("pi"))
                    {
                        tokens.Add(new Token(TokenType.Number, Mathf.Pi.ToString()));
                        pos += 2;
                    }
                    else
                    {
                        throw new Exception($"unknown function at {pos}");
                    }
                }
                else
                {
                    throw new Exception($"Unexpected character '{c}' at position {pos}");
                }
            }

            tokens.Add(new Token(TokenType.EOF, ""));
            return tokens;
        }

        /// <summary>
        /// Handle numeric or imaginary tokens, e.g. "3.14", "2.5i", ".5i", "i" alone, etc.
        /// </summary>
        private static void ParseNumberOrImag(string input, ref int pos, List<Token> tokens)
        {
            int startPos = pos;
            bool hasDot = false;
            bool hasDigits = false;
            bool hasI = false;

            // If the token is literally just 'i', treat it like "1i".
            if (input[pos] == 'i')
            {
                // e.g. "i" => "1.0" with imaginary = true
                tokens.Add(new Token(TokenType.Number, "1.0", true));
                pos++;
                return;
            }

            // Otherwise, gather digits, possibly one dot, and an optional trailing 'i'.
            while (pos < input.Length)
            {
                char nc = input[pos];
                if (char.IsDigit(nc))
                {
                    hasDigits = true;
                    pos++;
                }
                else if (nc == '.' && !hasDot)
                {
                    hasDot = true;
                    pos++;
                }
                else if (nc == 'i')
                {
                    hasI = true;
                    pos++;
                    break; // consume the 'i' and then stop
                }
                else
                {
                    // done
                    break;
                }
            }

            // If we never saw any digit (e.g. the input was just '.'?), that’s invalid.
            if (!hasDigits && !hasI && input[startPos] != '.')
            {
                throw new Exception($"Invalid numeric/imag token near '{input.Substring(startPos)}'");
            }

            // e.g. "3.14" or "2.5" or ".75"
            string numberText = input.Substring(startPos, pos - startPos - (hasI ? 1 : 0));
            // If the last char is '.', that might be invalid unless we allow "3." => "3.0"
            if (numberText.EndsWith("."))
            {
                numberText += "0";  // e.g. "3." => "3.0"
            }
            // If there's an 'i' we already consumed, so the token is imaginary
            tokens.Add(new Token(TokenType.Number, numberText, hasI));
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
        /// We also need to handle unary minus, which can appear before a Base.
        /// 
        /// A "Number" can be real or imaginary (e.g. "3" => vec2(3,0) or "2.5i" => vec2(0,2.5)).
        /// "z" is the complex variable, assumed to be vec2(x, y).
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
            /// Expression -> Term (("+"|"-") Term)*
            /// </summary>
            public AstNode ParseExpression()
            {
                AstNode left = ParseTerm();

                // Repeatedly parse ("+" Term) or ("-" Term)
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
            /// Term -> Factor (("*" Factor)|("/" Factor))*
            /// </summary>
            private AstNode ParseTerm()
            {
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
            /// Parse a Factor (handles exponent)
            /// Factor -> Base ("^" Factor)?
            /// 
            /// If there's a "^", we parse the exponent as another Factor.
            /// </summary>
            private AstNode ParseFactor()
            {
                // 1) Check for a leading unary minus on the entire factor
                if (Match(TokenType.Minus))
                {
                    Eat(TokenType.Minus);
                    // Recursively parse the next factor, so the minus applies to the whole thing
                    AstNode child = ParseFactor();
                    return new AstUnaryOp(child, true);
                }

                // 2) Parse the "base" part (number, z, function call, or parenthesized expression)
                AstNode baseNode = ParseBase();

                // 3) If we see '^', that indicates exponentiation
                //    We parse another Factor (not the whole Expression),
                //    so exponent has higher precedence than + or -.
                while (Match(TokenType.Caret))
                {
                    Eat(TokenType.Caret);
                    // Parse the exponent as another Factor
                    AstNode exponent = ParseFactor();
                    baseNode = new AstBinOp(baseNode, exponent, BinOpType.Power);
                }

                return baseNode;
            }


            /// <summary>
            /// Allow a leading unary minus (or plus) before a base,
            /// e.g. "-z", "+(3+2)", "-(1 + i2.5)", etc.
            /// </summary>
            private AstNode ParseUnary()
            {
                // If there's a leading minus, parse one
                // If there's a leading plus, skip it
                if (Match(TokenType.Minus))
                {
                    // This is a unary minus
                    Eat(TokenType.Minus);
                    // The next piece is the 'base' or a nested expression, but we need to wrap it in a node that negates it
                    AstNode child = ParseBase();
                    // Instead of a special AstUnaryMinus node, we can just multiply by -1.
                    // But let's do a small node for clarity:
                    return new AstUnaryOp(child, true);
                }
                else if (Match(TokenType.Plus))
                {
                    // If a leading plus, just skip it
                    Eat(TokenType.Plus);
                    AstNode child = ParseBase();
                    return child;
                }

                return ParseBase();
            }

            /// <summary>
            /// Parse a Base -> Number | z | "(" Expression ")"
            /// </summary>
            private AstNode ParseBase()
            {
                if (Current.Type == TokenType.Identifier)
                {
                    Token funcName = Eat(TokenType.Identifier); // e.g. "ln"
                    Eat(TokenType.LParen); // consume '('
                    AstNode arg = ParseExpression(); // the function argument
                    Eat(TokenType.RParen); // consume ')'
                    return new AstFunctionCall(funcName.Text, arg);
                }
                else if (Match(TokenType.LParen))
                {
                    Eat(TokenType.LParen);
                    AstNode expr = ParseExpression();
                    Eat(TokenType.RParen);
                    if (Current.Type == TokenType.EndIdentifier)
                    {
                        Token funcName = Eat(TokenType.EndIdentifier);
                        return new AstFunctionCall(funcName.Text, expr);
                    }
                    return expr;
                }
                else if (Match(TokenType.Number))
                {
                    Token t = Eat(TokenType.Number);
                    // Create an AstNumber, specifying if it's imaginary
                    // We'll parse the numeric text, then decide if it's real or imaginary
                    return new AstNumber(t.Text, t.IsImag);
                }
                else if (Match(TokenType.Z))
                {
                    Token t = Eat(TokenType.Z);
                    return new AstVariableZ();
                }
                else
                {
                    throw new Exception($"Unexpected token {Current.Type} at position {_pos} when expecting a base element");
                }
            }
        }
        private class AstFunctionCall : AstNode
        {
            public string FunctionName { get; }
            public AstNode Argument { get; }

            public AstFunctionCall(string functionName, AstNode argument)
            {
                FunctionName = functionName;
                Argument = argument;
            }

            public override string ToGlsl()
            {
                // Convert the argument to GLSL
                string argCode = Argument.ToGlsl();

                // Depending on the function name, call the appropriate function in your shader
                switch (FunctionName.ToLower())
                {
                    case "ln":
                        // We'll assume we have "vec2 complexLn(vec2 z)" in the shader
                        return $"complexLn({argCode})";

                    case "sin":
                        // We'll assume "vec2 complexSin(vec2 z)" in the shader
                        return $"complexSin({argCode})";
                    case "cos":
                        // We'll assume "vec2 complexSin(vec2 z)" in the shader
                        return $"complexCos({argCode})";
                    case "tan":
                        // We'll assume "vec2 complexSin(vec2 z)" in the shader
                        return $"complexTan({argCode})";
                    case "!":
                        return $"complexGamma({argCode})";

                    // etc...
                    default:
                        throw new Exception($"Unknown function {FunctionName}");
                }
            }
        }


        #endregion

        #region AST

        /// <summary>
        /// Base class for any node in the abstract syntax tree.
        /// Every node can produce a GLSL snippet with ToGlsl().
        /// </summary>
        private abstract class AstNode
        {
            public abstract string ToGlsl();
        }

        /// <summary>
        /// Represents a unary operation, specifically a leading minus (negation).
        /// </summary>
        private class AstUnaryOp : AstNode
        {
            private readonly AstNode _child;
            private readonly bool _isNegative;

            public AstUnaryOp(AstNode child, bool isNegative)
            {
                _child = child;
                _isNegative = isNegative;
            }

            public override string ToGlsl()
            {
                // If it’s a negation, multiply the child by -1.0
                if (_isNegative)
                {
                    return $"complexMult(vec2(-1.0, 0.0), {_child.ToGlsl()})";
                }
                // If not negative, just return the child
                return _child.ToGlsl();
            }
        }

        /// <summary>
        /// A numeric literal (possibly imaginary).
        /// For example, "3.14" => real part = 3.14, imag part = 0
        /// or "2.5" with isImag=true => real=0, imag=2.5
        /// </summary>
        private class AstNumber : AstNode
        {
            public double Value;
            public bool IsImag;

            public AstNumber(string text, bool isImag)
            {
                IsImag = isImag;

                // If the text is empty or ".", it’s invalid; your code can handle that if needed.
                // Convert string to double. Use InvariantCulture for '.' decimal separator.
                Value = double.Parse(
                    // handle case like ".5" => "0.5", if desired:
                    text.StartsWith(".") ? "0" + text : text,
                    CultureInfo.InvariantCulture
                );
            }

            public override string ToGlsl()
            {
                if (IsImag)
                {
                    // e.g. "2.5i" => vec2(0.0, 2.5)
                    return $"vec2(0.0, {Value.ToString("G", CultureInfo.InvariantCulture)})";
                }
                else
                {
                    // e.g. "3.14" => vec2(3.14, 0.0)
                    return $"vec2({Value.ToString("G", CultureInfo.InvariantCulture)}, 0.0)";
                }
            }
        }

        /// <summary>
        /// The variable 'z', treated as a complex vec2 in the shader.
        /// </summary>
        private class AstVariableZ : AstNode
        {
            public override string ToGlsl()
            {
                // Just return 'z' (lowercase assumed in your shader)
                // You can rename or adjust as you like.
                return "z";
            }
        }

        private enum BinOpType { Add, Subtract, Multiply, Divide, Power }

        /// <summary>
        /// Represents a binary operation like left + right, left ^ right, etc.
        /// We rely on external shader functions like complexAdd, complexSub, etc.
        /// </summary>
        private class AstBinOp : AstNode
        {
            private readonly AstNode _left;
            private readonly AstNode _right;
            private readonly BinOpType _op;

            public AstBinOp(AstNode left, AstNode right, BinOpType op)
            {
                _left = left;
                _right = right;
                _op = op;
            }

            public override string ToGlsl()
            {
                string leftCode = _left.ToGlsl();
                string rightCode = _right.ToGlsl();

                switch (_op)
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
