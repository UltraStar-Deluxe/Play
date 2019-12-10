using System;

namespace UniInject
{
    public class MissingBindingException : Exception
    {
        public MissingBindingException(string message) : base(message)
        {
        }
    }
}