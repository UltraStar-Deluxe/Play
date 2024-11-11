using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class GroupToken : Token {
        
        public GroupToken(IEnumerable<Token> tokens = null): base(TokenType.Group) {
            Tokens = tokens == null ? new Token[0] : tokens.ToArray();
        }

        public Token[] Tokens { get; }
    }
}
