using System;

namespace UniInject.Attributes
{
    [AttributeUsage(AttributeTargets.Field
                  | AttributeTargets.Property)]
    public class InjectComponentAttribute : Attribute
    {
        public GetComponentMethods GetComponentMethod { get; private set; }

        public InjectComponentAttribute(GetComponentMethods getComponentMethod)
        {
            this.GetComponentMethod = getComponentMethod;
        }
    }
}