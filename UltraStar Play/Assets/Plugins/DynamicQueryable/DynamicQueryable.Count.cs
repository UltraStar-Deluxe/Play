using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static int Count(this IQueryable source, string? predicate = null, params object[] values)
            => Count(source, predicate, null, values);

        public static int Count(this IQueryable source, string? predicate, VarType? variables, params object[] values)
            => (int)ExecuteOptionalExpression(source, "Count", predicate, string.IsNullOrEmpty(predicate), variables, values);

        public static long LongCount(this IQueryable source, string? predicate = null, params object[] values)
            => LongCount(source, predicate, null, values);

        public static long LongCount(this IQueryable source, string? predicate, VarType? variables, params object[] values)
            => (long)ExecuteOptionalExpression(source, "LongCount", predicate, string.IsNullOrEmpty(predicate), variables, values);
    }
}
