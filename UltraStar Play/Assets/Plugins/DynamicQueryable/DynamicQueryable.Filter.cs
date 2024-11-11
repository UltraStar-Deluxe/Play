using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static IQueryable<T> Where<T>(this IQueryable<T> source, string? predicate, params object[] values)
            => Where<T>(source, predicate, null, values);

        public static IQueryable<T> Where<T>(this IQueryable<T> source, string? predicate, VarType? variables, params object[] values)
            => (IQueryable<T>)Where((IQueryable)source, predicate, variables, values);

        public static IQueryable Where(this IQueryable source, string? predicate, params object[] values)
            => Where(source, predicate, null, values);

        public static IQueryable Where(IQueryable source, string? predicate, VarType? variables, params object[] values)
            => HandleLambda(source, "Where", predicate, false, variables, values);

        public static IQueryable<T> SkipWhile<T>(this IQueryable<T> source, string? predicate, params object[] values)
            => SkipWhile(source, predicate, null, values);

        public static IQueryable<T> SkipWhile<T>(this IQueryable<T> source, string? predicate, VarType? variables, params object[] values)
            => (IQueryable<T>)SkipWhile((IQueryable)source, predicate, variables, values);

        public static IQueryable SkipWhile(this IQueryable source, string? predicate, params object[] values)
            => SkipWhile(source, predicate, null, values);

        public static IQueryable SkipWhile(this IQueryable source, string? predicate, VarType? variables, params object[] values)
            => HandleLambda(source, "SkipWhile", predicate, false, variables, values);

        public static IQueryable<T> TakeWhile<T>(this IQueryable<T> source, string? predicate, params object[] values)
            => TakeWhile(source, predicate, null, values);

        public static IQueryable<T> TakeWhile<T>(this IQueryable<T> source, string? predicate, VarType? variables, params object[] values)
            => (IQueryable<T>)TakeWhile((IQueryable)source, predicate, variables, values);

        public static IQueryable TakeWhile(this IQueryable source, string? predicate, params object[] values)
            => TakeWhile(source, predicate, null, values);

        public static IQueryable TakeWhile(this IQueryable source, string? predicate, VarType? variables, params object[] values)
            => HandleLambda(source, "TakeWhile", predicate, false, variables, values);
    }
}
