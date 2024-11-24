using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class CallToken : Token {

        public CallToken(Token callee, IEnumerable<Token> args = null) : base(TokenType.Call) {
            Callee = callee;
            Args = args == null ? new Token[0] : args.ToArray();
        }
        
        public Token Callee { get; }
        public Token[] Args { get; }
    }
}
