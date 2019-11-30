using System;

namespace UniInject
{
    public class SingleInstanceProvider : NewInstancesProvider
    {
        private readonly Type type;

        private object singleInstance;

        public SingleInstanceProvider(Type type) : base(type)
        {
        }

        public override object Get()
        {
            if (singleInstance == null)
            {
                singleInstance = base.CreateInstance();
            }
            return singleInstance;
        }
    }
}