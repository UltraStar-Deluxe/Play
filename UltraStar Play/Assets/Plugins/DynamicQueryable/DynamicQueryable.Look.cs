using System.Collections.Generic;
using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static bool All(this IQueryable source, string? predicate = null, params object[] values)
            => All(source, predicate, null, values);

        public static bool All(this IQueryable source, string? predicate, VarType? variables, params object[] values)
            => (bool)ExecuteLambda(source, "All", predicate, false, variables, values);

        public static bool Any(this IQueryable source, string? predicate = null, params object[] values)
            => Any(source, predicate, null, values);

        public static bool Any(this IQueryable source, string? predicate, VarType? variables, params object[] values)
            => (bool)ExecuteOptionalExpression(source, "Any", predicate, string.IsNullOrEmpty(predicate), variables, values);

        public static bool Contains(this IQueryable source, object item)
            => (bool)ExecuteConstant(source, "Contains", item);

        public static bool SequenceEqual(this IQueryable source, IEnumerable<object> items)
            => (bool)ExecuteConstant(source, "SequenceEqual", items);
    }
}
