
using System;
using System.Reflection;

namespace UniInject
{

    public class InjectionData
    {
        // The type that needs injection. The member belongs to this object.
        public Type type;

        // The member of the target object that needs injection.
        public MemberInfo MemberInfo { get; private set; }

        // A method can have multiple parameters and all of them have to be injected.
        // Thus, there can be multiple injectionKeys for a member.
        public object[] InjectionKeys { get; private set; }

        public SearchMethods searchMethod;

        public bool isOptional;

        public InjectionData(Type type, MemberInfo memberInfo, object injectionKey, SearchMethods strategy, bool isOptional)
            : this(type, memberInfo, new object[] { injectionKey }, strategy, isOptional)
        {
        }

        public InjectionData(Type type, MemberInfo memberInfo, object[] injectionKeys, SearchMethods strategy, bool isOptional)
        {
            this.type = type;
            this.MemberInfo = memberInfo;
            this.InjectionKeys = injectionKeys;
            this.searchMethod = strategy;
            this.isOptional = isOptional;
        }
    }

}