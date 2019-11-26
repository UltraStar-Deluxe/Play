using System;

namespace UniInject
{
    public class MultipleBindingsException : Exception
    {
        public MultipleBindingsException(string message) : base(message)
        {
        }
    }
}