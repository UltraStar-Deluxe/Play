using System.Collections.Generic;
using System.Linq.Expressions;
using Jokenizer.Net;
using VarType = System.Collections.Generic.IDictionary<string, object>;
#nullable enable

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static IQueryable Join(this IQueryable outer, IQueryable inner, string outerKeySelector, string innerKeySelector, string resultSelector, params object[] values)
            => Join(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null, values);

        public static IQueryable Join(this IQueryable outer, IQueryable inner, string outerKeySelector, string innerKeySelector, string resultSelector, VarType? variables, params object[] values) {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (string.IsNullOrWhiteSpace(outerKeySelector)) throw new ArgumentNullException(nameof(outerKeySelector));
            if (string.IsNullOrWhiteSpace(innerKeySelector)) throw new ArgumentNullException(nameof(innerKeySelector));
            if (string.IsNullOrWhiteSpace(resultSelector)) throw new ArgumentNullException(nameof(resultSelector));

            var outerKeyLambda = Evaluator.ToLambda(outerKeySelector, new[] { outer.ElementType }, variables, values);
            var innerKeyLambda = Evaluator.ToLambda(innerKeySelector, new[] { inner.ElementType }, variables, values);
            var resultLambda = Evaluator.ToLambda(resultSelector, new[] { outer.ElementType, inner.ElementType }, variables, values);

            return outer.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Join",
                    new[] { outer.ElementType, inner.ElementType, outerKeyLambda.Body.Type, resultLambda.Body.Type },
                    outer.Expression,
                    Expression.Constant(inner),
                    Expression.Quote(outerKeyLambda),
                    Expression.Quote(innerKeyLambda),
                    Expression.Quote(resultLambda)
                )
            );
        }

        public static IQueryable GroupJoin(this IQueryable outer, IQueryable inner, string outerKeySelector, string innerKeySelector, string resultSelector, params object[] values)
            => GroupJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null, values);

        public static IQueryable GroupJoin(this IQueryable outer, IQueryable inner, string outerKeySelector, string innerKeySelector, string resultSelector, VarType? variables, params object[] values) {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (string.IsNullOrWhiteSpace(outerKeySelector)) throw new ArgumentNullException(nameof(outerKeySelector));
            if (string.IsNullOrWhiteSpace(innerKeySelector)) throw new ArgumentNullException(nameof(innerKeySelector));
            if (string.IsNullOrWhiteSpace(resultSelector)) throw new ArgumentNullException(nameof(resultSelector));

            var outerKeyLambda = Evaluator.ToLambda(outerKeySelector, new[] { outer.ElementType }, variables, values);
            var innerKeyLambda = Evaluator.ToLambda(innerKeySelector, new[] { inner.ElementType }, variables, values);
            var innerEnumType = typeof(IEnumerable<>).MakeGenericType(inner.ElementType);
            var resultLambda = Evaluator.ToLambda(resultSelector, new[] { outer.ElementType, innerEnumType }, variables, values);

            return outer.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "GroupJoin",
                    new[] { outer.ElementType, inner.ElementType, outerKeyLambda.Body.Type, resultLambda.Body.Type },
                    outer.Expression,
                    Expression.Constant(inner),
                    Expression.Quote(outerKeyLambda),
                    Expression.Quote(innerKeyLambda),
                    Expression.Quote(resultLambda)
                )
            );
        }

        public static IQueryable Zip<T>(this IQueryable first, IEnumerable<T> second, string resultSelector, params object[] values)
            => Zip(first, second, resultSelector, null, values);

        public static IQueryable Zip<T>(this IQueryable first, IEnumerable<T> second, string resultSelector, VarType? variables, params object[] values) {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (string.IsNullOrWhiteSpace(resultSelector)) throw new ArgumentNullException(nameof(resultSelector));

            var secondType = typeof(T);
            var resultLambda = Evaluator.ToLambda(resultSelector, new[] { first.ElementType, secondType }, variables, values);

            return first.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Zip",
                    new[] { first.ElementType, secondType, resultLambda.Body.Type },
                    first.Expression,
                    Expression.Constant(second),
                    Expression.Quote(resultLambda)
                )
            );
        }
    }
}
