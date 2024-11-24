namespace Jokenizer.Net.Tokens {

    public abstract class Token {

        protected Token(TokenType type) {
            Type = type;
        }
        
        public TokenType Type { get; }
    }
}
