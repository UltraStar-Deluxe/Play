namespace Jokenizer.Net.Tokens {

    public class LiteralToken : Token {

        public LiteralToken(object value): base(TokenType.Literal) {
            Value = value;
        }
        
        public object Value { get; }
    }
}
