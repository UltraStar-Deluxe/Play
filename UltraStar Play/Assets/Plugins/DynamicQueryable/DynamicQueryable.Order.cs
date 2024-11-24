using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string selector, params object[] values)
            => OrderBy(source, selector, null, values);

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string selector, VarType? variables, params object[] values)
            => (IQueryable<T>)OrderBy((IQueryable)source, selector, variables, values);

        public static IQueryable OrderBy(this IQueryable source, string selector, params object[] values)
            => OrderBy(source, selector, null, values);

        public static IQueryable OrderBy(this IQueryable source, string selector, VarType? variables, params object[] values)
            => HandleLambda(source, "OrderBy", selector, true, variables, values);

        public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string selector, params object[] values)
            => OrderByDescending(source, selector, null, values);

        public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string selector, VarType? variables, params object[] values)
            => (IQueryable<T>)OrderByDescending((IQueryable)source, selector, variables, values);

        public static IQueryable OrderByDescending(this IQueryable source, string selector, params object[] values)
            => OrderByDescending(source, selector, null, values);

        public static IQueryable OrderByDescending(this IQueryable source, string selector, VarType? variables, params object[] values)
            => HandleLambda(source, "OrderByDescending", selector, true, variables, values);

        public static IQueryable<T> ThenBy<T>(this IQueryable<T> source, string selector, params object[] values)
            => ThenBy(source, selector, null, values);

        public static IQueryable<T> ThenBy<T>(this IQueryable<T> source, string selector, VarType? variables, params object[] values)
            => (IQueryable<T>)ThenBy((IQueryable)source, selector, variables, values);

        public static IQueryable ThenBy(this IQueryable source, string selector, params object[] values)
            => ThenBy(source, selector, null, values);

        public static IQueryable ThenBy(this IQueryable source, string selector, VarType? variables, params object[] values)
            => HandleLambda(source, "ThenBy", selector, true, variables, values);

        public static IQueryable<T> ThenByDescending<T>(this IQueryable<T> source, string selector, params object[] values)
            => ThenByDescending(source, selector, null, values);

        public static IQueryable<T> ThenByDescending<T>(this IQueryable<T> source, string selector, VarType? variables, params object[] values)
            => (IQueryable<T>)ThenByDescending((IQueryable)source, selector, variables, values);

        public static IQueryable ThenByDescending(this IQueryable source, string selector, params object[] values)
            => ThenByDescending(source, selector, null, values);

        public static IQueryable ThenByDescending(this IQueryable source, string selector, VarType? variables, params object[] values)
            => HandleLambda(source, "ThenByDescending", selector, true, variables, values);
    }
}
