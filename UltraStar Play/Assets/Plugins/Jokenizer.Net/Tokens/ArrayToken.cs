using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Tokens {

    public class ArrayToken : Token {

        public ArrayToken(IEnumerable<Token> items): base(TokenType.Array) {
            Items = items == null ? new AssignToken[0] : items.ToArray();
        }
        
        public Token[] Items { get; }
    }
}
