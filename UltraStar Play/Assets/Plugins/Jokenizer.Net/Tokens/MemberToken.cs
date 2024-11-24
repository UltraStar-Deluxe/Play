namespace Jokenizer.Net.Tokens {

    public class MemberToken : Token, IVariableToken {

        public MemberToken(Token owner, string name) : base(TokenType.Member) {
            Owner = owner;
            Name = name;
        }

        public Token Owner { get; }
        public string Name { get; }
    }
}
