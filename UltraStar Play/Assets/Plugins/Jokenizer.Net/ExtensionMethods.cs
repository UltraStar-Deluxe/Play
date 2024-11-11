using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jokenizer.Net {

    public static class ExtensionMethods {
        private static HashSet<MethodInfo> extensions = new HashSet<MethodInfo>();

        static ExtensionMethods() {
            ScanTypes(typeof(Queryable), typeof(Enumerable));
        }

        public static IList<MethodInfo> ProbeAllAssemblies() {
            return ProbeAssemblies(Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load));
        }

        public static IList<MethodInfo> ProbeAssemblies(params Assembly[] assemblies) => assemblies.SelectMany(ProbeAssembly).ToList();
        public static IList<MethodInfo> ProbeAssemblies(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(ProbeAssembly).ToList();

        public static IList<MethodInfo> ProbeAssembly(Assembly assembly) {
            return ScanTypes(assembly.GetTypes().Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested));
        }

        public static IList<MethodInfo> ScanTypes(params Type[] types) => types.SelectMany(ScanType).ToList();
        public static IList<MethodInfo> ScanTypes(IEnumerable<Type> types) => types.SelectMany(ScanType).ToList();

        public static IList<MethodInfo> ScanType(Type type) {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
                .ToList();

            methods.ForEach(m => extensions.Add(m));

            return methods;
        }

        public static MethodInfo Find(Type forType, string name, Expression[] availableArgs) {
            var args = forType.IsConstructedGenericType ? forType.GetGenericArguments() : new Type[0];

            foreach (var extension in extensions) {
                var m = extension;
                if (m.Name != name) continue;

                if (m.IsGenericMethodDefinition) {
                    if (m.GetGenericArguments().Length != args.Length) continue;
                    
                    m = m.MakeGenericMethod(args);
                }

                var prms = m.GetParameters();
                if (!prms[0].ParameterType.IsAssignableFrom(forType)) continue;

                if (!Helper.IsSuitable(prms.Skip(1).ToArray(), availableArgs)) continue;
                
                return m;
            }

            return null;
        }
    }
}
