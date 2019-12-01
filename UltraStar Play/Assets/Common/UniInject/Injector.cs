using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UniInject
{
    public class Injector
    {
        // The parent Injector.
        // If a binding is not found in this Injector, then it is searched in the parent injectors recursively.
        public Injector ParentInjector { get; private set; }

        private readonly List<IBinding> bindings = new List<IBinding>();

        private readonly HashSet<Type> getValuesForConstructorInjectionVisitedTypes = new HashSet<Type>();
        private readonly Dictionary<object, object> injectionKeyToObjectWithOngoingInjectionMap = new Dictionary<object, object>();

        private readonly List<UnitySearchMethodMockup> unitySearchMethodMockups = new List<UnitySearchMethodMockup>();

        internal Injector(Injector parent)
        {
            this.ParentInjector = parent;
        }

        public T GetInstance<T>()
        {
            object result = GetInstance(typeof(T));
            if (result is T t)
            {
                return t;
            }
            throw new InjectionException($"Cannot create instance of type {typeof(T)}. No binding found.");
        }

        public object GetInstance(object bindingKey)
        {
            IBinding binding = GetBinding(bindingKey);
            IProvider provider = binding.GetProvider();

            // A provider that creates new instances must be able
            // to resolve constructor parameters from the injector's context.
            object result = provider.Get(this, out bool resultNeedsInjection);

            // If the result is newly created, then it has to be injected as well.
            if (resultNeedsInjection)
            {
                // For circular dependencies, the object that is currently created for the injectionKey is stored temporarily.
                // The map is prioritized when resolving dependencies.
                // Thus, further newly created objects (i.e. dependencies of the result that is constructed here)
                // that have a dependency to injectionKey (i.e. to the result that is constructed here),
                // can have the object injected that has already been instantiated here.
                injectionKeyToObjectWithOngoingInjectionMap.Add(bindingKey, result);
                Inject(result);
                injectionKeyToObjectWithOngoingInjectionMap.Remove(bindingKey);
            }

            return result;
        }

        public void Inject(object target)
        {
            // Find all members to be injected via reflection.
            List<InjectionData> injectionDatas = ReflectionUtils.CreateInjectionDatas(target);

            // Inject existing bindings into the fields.
            foreach (InjectionData injectionData in injectionDatas)
            {
                Inject(injectionData);
            }
        }

        public void Inject(InjectionData injectionData)
        {
            if (injectionData.searchMethod == SearchMethods.SearchInBindings)
            {
                InjectMemberFromBindings(injectionData.TargetObject, injectionData.MemberInfo, injectionData.InjectionKeys, injectionData.isOptional);
            }
            else if (injectionData.TargetObject is MonoBehaviour)
            {
                InjectMemberFromUnitySearchMethod(injectionData.TargetObject as MonoBehaviour, injectionData.MemberInfo, injectionData.searchMethod, injectionData.isOptional);
            }
            else
            {
                throw new InjectionException($"Cannot perform injection via {injectionData.searchMethod} into an object of type {injectionData.TargetObject.GetType()}."
                    + " Only MonoBehaviour instances are supported.");
            }
        }

        private void InjectMemberFromBindings(object target, MemberInfo memberInfo, object[] bindingKeys, bool isOptional)
        {
            object[] valuesToBeInjected;
            try
            {
                if (isOptional)
                {
                    try
                    {
                        valuesToBeInjected = GetValuesToBeInjected(bindingKeys);
                    }
                    catch (MissingBindingException)
                    {
                        // Ignore because the injection is optional.
                        return;
                    }
                }
                else
                {
                    valuesToBeInjected = GetValuesToBeInjected(bindingKeys);
                }

                if (valuesToBeInjected == null)
                {
                    throw new InjectionException("No values to be injected.");
                }

                if (memberInfo is FieldInfo)
                {
                    (memberInfo as FieldInfo).SetValue(target, valuesToBeInjected[0]);
                }
                else if (memberInfo is PropertyInfo)
                {
                    (memberInfo as PropertyInfo).SetValue(target, valuesToBeInjected[0]);
                }
                else if (memberInfo is MethodInfo)
                {
                    (memberInfo as MethodInfo).Invoke(target, valuesToBeInjected);
                }
                else
                {
                    throw new InjectionException($"Only Fields, Properties and Methods are supported for injection.");
                }

            }
            catch (Exception e)
            {
                throw new InjectionException($"Cannot inject {target}.{memberInfo.Name}: " + e.Message, e);
            }
        }

        private void InjectMemberFromUnitySearchMethod(MonoBehaviour script, MemberInfo memberInfo, SearchMethods searchMethod, bool isOptional)
        {
            Type componentType = ReflectionUtils.GetTypeOfFieldOrProperty(script, memberInfo);

            // For testing, searching in the scene hierarchy using a Unity method can be simulated to return a mockup for a component.
            object component = GetComponentFromUnitySearchMethodMockups(script, searchMethod, componentType);
            if (component == null)
            {
                // No mockup found, thus use the real Unity search method.
                component = GetComponentFromUnitySearchMethod(script, searchMethod, componentType);
            }

            if (component != null)
            {
                if (memberInfo is FieldInfo)
                {
                    (memberInfo as FieldInfo).SetValue(script, component);
                }
                else if (memberInfo is PropertyInfo)
                {
                    (memberInfo as PropertyInfo).SetValue(script, component);
                }
                else
                {
                    throw new Exception($"Cannot inject member {script.name}.{memberInfo}."
                        + $" Only Fields and Properties are supported for component injection via Unity methods.");
                }
            }
            else if (!isOptional)
            {
                throw new Exception($"Cannot inject member {script.name}.{memberInfo.Name}."
                    + $" No component of type {componentType} found using method {searchMethod}");
            }
        }

        private object GetComponentFromUnitySearchMethodMockups(MonoBehaviour script, SearchMethods searchMethod, Type componentType)
        {
            foreach (UnitySearchMethodMockup unitySearchMethodMockup in unitySearchMethodMockups)
            {
                Type mockedSearchReturnType = unitySearchMethodMockup.searchResult.GetType();
                bool callingScriptMatches = (unitySearchMethodMockup.callingScript == null || unitySearchMethodMockup.callingScript == script);
                bool returnTypeMatches = componentType.IsAssignableFrom(mockedSearchReturnType);
                if (callingScriptMatches && returnTypeMatches)
                {
                    return unitySearchMethodMockup.searchResult;
                }
            }
            return null;
        }

        private UnityEngine.Object GetComponentFromUnitySearchMethod(MonoBehaviour script, SearchMethods searchMethod, Type componentType)
        {
            switch (searchMethod)
            {
                case SearchMethods.GetComponent:
                    return script.GetComponent(componentType);
                case SearchMethods.GetComponentInChildren:
                    return script.GetComponentInChildren(componentType);
                case SearchMethods.GetComponentInParent:
                    return script.GetComponentInParent(componentType);
                case SearchMethods.FindObjectOfType:
                    return GameObject.FindObjectOfType(componentType);
                default:
                    throw new InjectionException($" Unkown Unity search method {searchMethod}");
            }
        }

        internal object[] GetValuesForConstructorInjection(Type type)
        {
            if (getValuesForConstructorInjectionVisitedTypes.Contains(type))
            {
                throw new InjectionException($"Circular dependencies in the constructor parameters of type {type}");
            }
            getValuesForConstructorInjectionVisitedTypes.Add(type);

            ConstructorInjectionData constructorInjectionData = UniInject.GetConstructorInjectionData(type);
            object[] bindingKeys = constructorInjectionData.InjectionKeys;
            object[] result = GetValuesToBeInjected(bindingKeys);

            getValuesForConstructorInjectionVisitedTypes.Remove(type);
            return result;
        }

        private object[] GetValuesToBeInjected(object[] bindingKeys)
        {
            if (bindingKeys == null)
            {
                return null;
            }

            object[] valuesToBeInjected = new object[bindingKeys.Length];
            int index = 0;
            foreach (object bindingKey in bindingKeys)
            {
                // Lookup in special map to resolve circular dependencies.
                // It checks if there is already an object for the bindingKey that has been instantiated, but is currently injected with its own dependencies.
                bool valueToBeInjectedFound = injectionKeyToObjectWithOngoingInjectionMap.TryGetValue(bindingKey, out object valueToBeInjected);
                if (!valueToBeInjectedFound)
                {
                    // Get (possibly newly created) instance.
                    valueToBeInjected = GetInstance(bindingKey);
                }

                valuesToBeInjected[index] = valueToBeInjected ?? throw new InjectionException($"Value to be injected for key {bindingKey} is null");
                index++;
            }
            return valuesToBeInjected;
        }

        private IBinding GetBinding(object bindingKey)
        {
            List<IBinding> matchingBindings = bindings.Where(it => it.GetKey().Equals(bindingKey)).ToList();
            if (matchingBindings.Count == 0)
            {
                if (ParentInjector != null)
                {
                    return ParentInjector.GetBinding(bindingKey);
                }
                throw new MissingBindingException("Missing binding for key " + bindingKey);
            }
            else if (matchingBindings.Count > 1)
            {
                throw new MultipleBindingsException("Multiple bindings for key " + bindingKey);
            }

            return matchingBindings[0];
        }

        public void AddBinding(IBinding binding)
        {
            bindings.Add(binding);
        }

        public void MockUnitySearchMethod(MonoBehaviour callingScript, SearchMethods searchMethod, object searchResult)
        {
            UnitySearchMethodMockup unitySearchMethodMockup = new UnitySearchMethodMockup(callingScript, searchMethod, searchResult);
            unitySearchMethodMockups.Add(unitySearchMethodMockup);
        }

        private class UnitySearchMethodMockup
        {
            public MonoBehaviour callingScript;
            public SearchMethods searchMethod;
            public object searchResult;

            public UnitySearchMethodMockup(MonoBehaviour callingScript, SearchMethods searchMethod, object mockup)
            {
                this.callingScript = callingScript;
                this.searchMethod = searchMethod;
                this.searchResult = mockup;
            }
        }
    }
}
