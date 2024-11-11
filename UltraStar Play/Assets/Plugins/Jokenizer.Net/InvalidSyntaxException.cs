using System;

namespace Jokenizer.Net {

    public class InvalidSyntaxException : Exception {

        public InvalidSyntaxException(string message): base(message) {
        }
    }
}
