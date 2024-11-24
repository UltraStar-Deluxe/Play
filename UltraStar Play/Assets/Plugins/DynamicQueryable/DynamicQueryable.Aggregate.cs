using System.Linq.Expressions;
using Jokenizer.Net;
using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static object Average(this IQueryable source, string? selector = null, params object[] values)
            => Average(source, selector, null, values);

        public static object Average(this IQueryable source, string? selector, VarType? variables, params object[] values)
            => ExecuteOptionalExpression(source, "Average", selector, false, variables, values);

        public static object Sum(this IQueryable source, string? selector = null, params object[] values)
            => Sum(source, selector, null, values);

        public static object Sum(this IQueryable source, string? selector, VarType? variables, params object[] values)
            => ExecuteOptionalExpression(source, "Sum", selector, false, variables, values);

        public static object Max(this IQueryable source, string? selector = null, params object[] values)
            => Max(source, selector, null, values);

        public static object Max(this IQueryable source, string? selector, VarType? variables, params object[] values)
            => ExecuteOptionalExpression(source, "Max", selector, true, variables, values);

        public static object Min(this IQueryable source, string? selector = null, params object[] values)
            => Min(source, selector, null, values);

        public static object Min(this IQueryable source, string? selector, VarType? variables, params object[] values)
            => ExecuteOptionalExpression(source, "Min", selector, true, variables, values);

        public static object Aggregate(this IQueryable source, string func, params object[] values)
            => Aggregate(source, func, null, values);

        public static object Aggregate(this IQueryable source, string func, VarType? variables, params object[] values) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(func)) throw new ArgumentNullException(nameof(func));

            var funcLambda = Evaluator.ToLambda(func, new[] { source.ElementType, source.ElementType }, variables, values);

            return source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable),
                    "Aggregate",
                    new[] { source.ElementType },
                    source.Expression,
                    Expression.Quote(funcLambda)
                )
            );
        }

        public static object Aggregate(this IQueryable source, object seed, string func, params object[] values)
            => Aggregate(source, seed, func, (VarType?)null, values);

        public static object Aggregate(this IQueryable source, object seed, string func, VarType? variables, params object[] values) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (string.IsNullOrWhiteSpace(func)) throw new ArgumentNullException(nameof(func));

            var funcLambda = Evaluator.ToLambda(func, new[] { seed.GetType(), source.ElementType }, variables, values);

            return source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable),
                    "Aggregate",
                    new[] { source.ElementType, seed.GetType() },
                    source.Expression,
                    Expression.Constant(seed),
                    Expression.Quote(funcLambda)
                )
            );
        }

        public static object Aggregate(this IQueryable source, object seed, string func, string selector, params object[] values)
            => Aggregate(source, seed, func, selector, null, values);

        public static object Aggregate(this IQueryable source, object seed, string func, string? selector, VarType? variables, params object[] values) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (string.IsNullOrWhiteSpace(func)) throw new ArgumentNullException(nameof(func));
            if (string.IsNullOrWhiteSpace(selector)) throw new ArgumentNullException(nameof(func));

            var funcLambda = Evaluator.ToLambda(func, new[] { seed.GetType(), source.ElementType }, variables, values);
            var selectorLambda = Evaluator.ToLambda(selector, new[] { seed.GetType() }, variables, values);

            return source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable),
                    "Aggregate",
                    new[] { source.ElementType, seed.GetType(), selectorLambda.Body.Type },
                    source.Expression,
                    Expression.Constant(seed),
                    Expression.Quote(funcLambda),
                    Expression.Quote(selectorLambda)
                )
            );
        }
    }
}
