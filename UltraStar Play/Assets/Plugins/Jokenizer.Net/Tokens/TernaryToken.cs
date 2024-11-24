namespace Jokenizer.Net.Tokens {

    public class TernaryToken : Token {

        public TernaryToken(Token predicate, Token whenTrue, Token whenFalse) : base(TokenType.Ternary) {
            Predicate = predicate;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        public Token Predicate { get; }
        public Token WhenTrue { get; }
        public Token WhenFalse { get; }
    }
}
