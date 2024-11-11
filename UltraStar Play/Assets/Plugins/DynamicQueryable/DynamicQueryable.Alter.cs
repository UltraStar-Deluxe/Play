namespace System.Linq.Dynamic {

    public static partial class DynamicQueryable {

        public static IQueryable Take(this IQueryable source, int count)
            => HandleConstant(source, "Take", count);

        public static IQueryable Skip(this IQueryable source, int count)
            => HandleConstant(source, "Skip", count);

        public static IQueryable Distinct(this IQueryable source)
            => Handle(source, "Distinct");

        public static IQueryable Reverse(this IQueryable source)
            => Handle(source, "Reverse");

        public static IQueryable DefaultIfEmpty(this IQueryable source)
            => Handle(source, "DefaultIfEmpty");

        public static IQueryable DefaultIfEmpty(this IQueryable source, object defaultValue)
            => HandleConstant(source, "DefaultIfEmpty", defaultValue);
    }
}
