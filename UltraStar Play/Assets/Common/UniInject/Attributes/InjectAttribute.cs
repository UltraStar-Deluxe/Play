using System;

namespace UniInject.Attributes
{
    [AttributeUsage(AttributeTargets.Constructor
                  | AttributeTargets.Field
                  | AttributeTargets.Property
                  | AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {
        public object key;

        public InjectAttribute()
        {
        }
    }
}