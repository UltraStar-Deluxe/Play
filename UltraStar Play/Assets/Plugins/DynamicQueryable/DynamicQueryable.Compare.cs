using System.Collections.Generic;

namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static IQueryable Except<T>(this IQueryable source, IEnumerable<T> items)
            => HandleConstant(source, "Except", items);

        public static IQueryable Intersect<T>(this IQueryable source, IEnumerable<T> items)
            => HandleConstant(source, "Intersect", items);

        public static IQueryable Union<T>(this IQueryable source, IEnumerable<T> items)
            => HandleConstant(source, "Union", items);

        public static IQueryable Concat<T>(this IQueryable source, IEnumerable<T> items)
            => HandleConstant(source, "Concat", items);
    }
}
