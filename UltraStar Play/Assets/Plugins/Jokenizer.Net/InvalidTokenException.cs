using System;

namespace Jokenizer.Net {

    public class InvalidTokenException : Exception {

        public InvalidTokenException(string message): base(message) {
        }
    }
}
