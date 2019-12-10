using System;

namespace UniInject
{
    [AttributeUsage(AttributeTargets.Constructor
                  | AttributeTargets.Field
                  | AttributeTargets.Property
                  | AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {
        public object key;

        public bool optional;

        public SearchMethods searchMethod = SearchMethods.SearchInBindings;

        public InjectAttribute()
        {
        }
    }
}