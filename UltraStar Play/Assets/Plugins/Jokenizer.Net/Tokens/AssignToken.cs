namespace Jokenizer.Net.Tokens {

    public class AssignToken : Token, IVariableToken {

        public AssignToken(string name, Token right) : base(TokenType.Assign) {
            Name = name;
            Right = right;
        }

        public string Name { get; }
        public Token Right { get; }
    }
}
