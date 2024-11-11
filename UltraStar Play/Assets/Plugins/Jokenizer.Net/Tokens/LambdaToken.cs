using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class LambdaToken : Token {

        public LambdaToken(Token body, IEnumerable<string> parameters = null) : base(TokenType.Lambda) {
            Body = body;
            Parameters = parameters == null ? new string[0] : parameters.ToArray();
        }

        public Token Body { get; }
        public string[] Parameters { get; }
    }
}
