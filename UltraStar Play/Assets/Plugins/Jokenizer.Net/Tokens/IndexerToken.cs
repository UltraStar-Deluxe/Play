namespace Jokenizer.Net.Tokens {

    public class IndexerToken : Token {

        public IndexerToken(Token owner, Token key) : base(TokenType.Indexer) {
            Owner = owner;
            Key = key;
        }

        public Token Owner { get; }
        public Token Key { get; }
    }
}
