using System;

namespace UniInject.Attributes
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