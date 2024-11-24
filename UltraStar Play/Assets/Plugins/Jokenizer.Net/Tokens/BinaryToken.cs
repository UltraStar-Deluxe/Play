namespace Jokenizer.Net.Tokens {

    public class BinaryToken : Token {

        public BinaryToken(string op, Token left, Token right) : base(TokenType.Binary) {
            Operator = op;
            Left = left;
            Right = right;
        }

        public string Operator { get; }
        public Token Left { get; }
        public Token Right { get; }
    }
}
