using System;

namespace UniInject
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class InjectionKeyAttribute : Attribute
    {
        public object key;

        public InjectionKeyAttribute(string key)
        {
            this.key = key;
        }
    }
}