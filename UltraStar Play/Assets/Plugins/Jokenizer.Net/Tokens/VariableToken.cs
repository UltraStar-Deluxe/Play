namespace Jokenizer.Net.Tokens {

    public class VariableToken : Token, IVariableToken {

        public VariableToken(string name) : base(TokenType.Variable) {
            Name = name;
        }

        public string Name { get; }
    }

    public interface IVariableToken {
        string Name { get; }
    }
}
