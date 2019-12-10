using System;

namespace UniInject
{
    public class InjectionException : Exception
    {
        public InjectionException(string message) : base(message)
        {
        }

        public InjectionException(string message, Exception e) : base(message, e)
        {
        }
    }
}