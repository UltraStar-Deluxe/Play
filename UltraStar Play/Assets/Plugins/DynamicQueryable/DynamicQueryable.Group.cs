using System.Collections.Generic;
using System.Linq.Expressions;
using Jokenizer.Net;
using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static IQueryable GroupBy(this IQueryable source, string keySelector, string elementSelector, string resultSelector, params object[] values)
            => GroupBy(source, keySelector, elementSelector, resultSelector, null, values);

        public static IQueryable GroupBy(this IQueryable source, string keySelector, string elementSelector, string resultSelector, VarType? variables, params object[] values) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(keySelector)) throw new ArgumentNullException(nameof(keySelector));
            if (string.IsNullOrWhiteSpace(elementSelector)) throw new ArgumentNullException(nameof(elementSelector));
            if (string.IsNullOrWhiteSpace(resultSelector)) throw new ArgumentNullException(nameof(resultSelector));

            var keyLambda = Evaluator.ToLambda(keySelector, new[] { source.ElementType }, variables, values);
            var elementLambda = Evaluator.ToLambda(elementSelector, new[] { source.ElementType }, variables, values);
            var enumElementType = typeof(IEnumerable<>).MakeGenericType(elementLambda.Body.Type);
            var resultLambda = Evaluator.ToLambda(resultSelector, new[] { keyLambda.Body.Type, enumElementType }, variables, values);

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "GroupBy",
                    new[] { source.ElementType, keyLambda.Body.Type, elementLambda.Body.Type, resultLambda.Body.Type },
                    source.Expression,
                    Expression.Quote(keyLambda),
                    Expression.Quote(elementLambda),
                    Expression.Quote(resultLambda)
                )
            );
        }

        public static IQueryable GroupBy(this IQueryable source, string keySelector, string resultSelector, params object[] values)
            => GroupBy(source, keySelector, resultSelector, (VarType?)null, values);

        public static IQueryable GroupBy(this IQueryable source, string keySelector, string resultSelector, VarType? variables, params object[] values) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(keySelector)) throw new ArgumentNullException(nameof(keySelector));
            if (string.IsNullOrWhiteSpace(resultSelector)) throw new ArgumentNullException(nameof(resultSelector));

            var keyLambda = Evaluator.ToLambda(keySelector, new[] { source.ElementType }, variables, values);
            var enumSourceType = typeof(IEnumerable<>).MakeGenericType(source.ElementType);
            var resultLambda = Evaluator.ToLambda(resultSelector, new[] { keyLambda.Body.Type, enumSourceType }, variables, values);

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "GroupBy",
                    new[] { source.ElementType, keyLambda.Body.Type, resultLambda.Body.Type },
                    source.Expression,
                    Expression.Quote(keyLambda),
                    Expression.Quote(resultLambda)
                )
            );
        }

        public static IQueryable GroupBy(this IQueryable source, string keySelector, params object[] values)
            => GroupBy(source, keySelector, (VarType?)null, values);

        public static IQueryable GroupBy(this IQueryable source, string keySelector, VarType? variables, params object[] values)
            => HandleLambda(source, "GroupBy", keySelector, true, variables, values);
    }
}