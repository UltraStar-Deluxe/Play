namespace Jokenizer.Net.Tokens {

    public class UnaryToken : Token {

        public UnaryToken(char op, Token target): base(TokenType.Unary) {
            Operator = op;
            Target = target;
        }
        
        public char Operator { get; }
        public Token Target { get; }
    }
}
