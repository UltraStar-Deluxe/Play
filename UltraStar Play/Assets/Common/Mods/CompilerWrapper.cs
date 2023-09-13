// inspiration:
// https://gist.github.com/SeargeDP/967f007ac896accfc214
// http://blog.davidebbo.com/2012/02/quick-fun-with-monos-csharp-compiler-as.html
// https://www.reddit.com/r/gamedev/comments/2zvlm1/sandbox_solution_for_c_scripts_using_monocsharp/
// http://www.amazedsaint.com/2010/09/c-as-scripting-language-in-your-net.html


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mono.CSharp;
using Attribute = System.Attribute;
using Delegate = System.Delegate;
using Enum = System.Enum;

public class CompilerWrapper
{
    private readonly Evaluator evaluator;
    private readonly CompilerContext context;
    private readonly CompilerWrapperReportPrinter reportPrinter;

    public string PartialReport => reportPrinter.PartialReport;
    public int PartialReportErrorCount => reportPrinter.PartialReportErrorCount;
    public int PartialReportWarningCount => reportPrinter.PartialReportWarningCount;

    public string FullReport => reportPrinter.FullReport;
    public int FullReportErrorCount => reportPrinter.FullReportErrorCount;
    public int FullReportWarningCount => reportPrinter.FullReportWarningCount;

    public CompilerWrapper () {
        // create new settings that will *not* load up all of standard lib by default
        // see: https://github.com/mono/mono/blob/master/mcs/mcs/settings.cs

        CompilerSettings settings = new CompilerSettings
        {
            LoadDefaultReferences = false,
            StdLib = false,
            Encoding = Encoding.UTF8,
            EnhancedWarnings = true,
            Checked = true,
            Stacktrace = true,
            // GenerateDebugInfo = true,
        };
        this.reportPrinter = new CompilerWrapperReportPrinter();
        this.context = new CompilerContext(settings, reportPrinter);

        this.evaluator = new Evaluator(context);

        // ImportAllowedTypes(BuiltInTypes, AdditionalTypes, QuestionableTypes);
        ImportAllowedTypes(BuiltInTypes, AdditionalTypes);
    }

    public void ReferenceAssembly(Assembly assembly)
    {
        this.evaluator.ReferenceAssembly(assembly);
    }

    /// <summary> Evaluates code. Returns true on successful evaluation, or false on errors. </summary>
    public bool EvaluateCode(string code)
    {
        return evaluator.Run(code);
    }

    public void StartNewPartialReport()
    {
        reportPrinter.StartNewPartialReport();
    }

    private void ImportAllowedTypes (params Type[][] allowedTypeArrays) {
        // expose Evaluator.importer and Evaluator.module
        var evtype = typeof(Evaluator);
        var importer = (ReflectionImporter)evtype
            .GetField("importer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(evaluator);
        var module = (ModuleContainer)evtype
            .GetField("module", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(evaluator);

        // expose MetadataImporter.ImportTypes(Type[], RootNamespace, bool)
        var importTypes = importer.GetType().GetMethod(
            "ImportTypes", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any,
            new Type[] { typeof(Type[]), typeof(Namespace), typeof(bool) }, null);

        foreach (Type[] types in allowedTypeArrays)
        {
            importTypes.Invoke(importer, new object[] { types, module.GlobalRootNamespace, false });
        }
    }

    #region Allowed Types

    /// <summary>
    /// Basic built-in system types.
    /// </summary>
    private static Type[] BuiltInTypes = new Type[] {
        typeof(void),
        typeof(Type),
        typeof(Object),
        typeof(ValueType),
        typeof(Array),

        typeof(SByte),
        typeof(Byte),
        typeof(Int16),
        typeof(UInt16),
        typeof(Int32),
        typeof(UInt32),
        typeof(Int64),
        typeof(UInt64),
        typeof(Single),
        typeof(Double),
        typeof(Char),
        typeof(String),
        typeof(Boolean),
        typeof(Decimal),
        typeof(IntPtr),
        typeof(UIntPtr),
        typeof(Enum),
        typeof(Attribute),
        typeof(Delegate),
        typeof(MulticastDelegate),
        typeof(IDisposable),
        typeof(Exception),
        typeof(RuntimeFieldHandle),
        typeof(RuntimeTypeHandle),
        typeof(ParamArrayAttribute),
        typeof(OutAttribute),
    };

    /// <summary>
    /// These types may be useful in scripts but they're not strictly necessary and
    /// should be edited as desired.
    /// </summary>
    private static Type[] AdditionalTypes = new Type[] {

        // mscorlib System

        typeof(Action),
        typeof(Action<>),
        typeof(Action<,>),
        typeof(Action<,,>),
        typeof(Action<,,,>),
        typeof(ArgumentException),
        typeof(ArgumentNullException),
        typeof(ArgumentOutOfRangeException),
        typeof(ArithmeticException),
        typeof(ArraySegment<>),
        typeof(ArrayTypeMismatchException),
        typeof(Comparison<>),
        typeof(Convert),
        typeof(Converter<,>),
        typeof(DivideByZeroException),
        typeof(FlagsAttribute),
        typeof(FormatException),
        typeof(Func<>),
        typeof(Func<,>),
        typeof(Func<,,>),
        typeof(Func<,,,>),
        typeof(Func<,,,,>),
        typeof(Guid),
        typeof(IAsyncResult),
        typeof(ICloneable),
        typeof(IComparable),
        typeof(IComparable<>),
        typeof(IConvertible),
        typeof(ICustomFormatter),
        typeof(IEquatable<>),
        typeof(IFormatProvider),
        typeof(IFormattable),
        typeof(IndexOutOfRangeException),
        typeof(InvalidCastException),
        typeof(InvalidOperationException),
        typeof(InvalidTimeZoneException),
        typeof(Math),
        typeof(MidpointRounding),
        typeof(NonSerializedAttribute),
        typeof(NotFiniteNumberException),
        typeof(NotImplementedException),
        typeof(NotSupportedException),
        typeof(Nullable),
        typeof(Nullable<>),
        typeof(NullReferenceException),
        typeof(ObjectDisposedException),
        typeof(ObsoleteAttribute),
        typeof(OverflowException),
        typeof(Predicate<>),
        typeof(Random),
        typeof(RankException),
        typeof(SerializableAttribute),
        typeof(StackOverflowException),
        typeof(StringComparer),
        typeof(StringComparison),
        typeof(StringSplitOptions),
        typeof(SystemException),
        typeof(TimeoutException),
        typeof(TypeCode),
        typeof(Version),
        typeof(WeakReference),

        // mscorlib System.Collections

        typeof(BitArray),
        typeof(ICollection),
        typeof(IComparer),
        typeof(IDictionary),
        typeof(IDictionaryEnumerator),
        typeof(IEqualityComparer),
        typeof(IList),

        // mscorlib System.Collections.Generic

        typeof(IEnumerator),
        typeof(IEnumerable),
        typeof(Comparer<>),
        typeof(Dictionary<,>),
        typeof(EqualityComparer<>),
        typeof(ICollection<>),
        typeof(IComparer<>),
        typeof(IDictionary<,>),
        typeof(IReadOnlyDictionary<,>),
        typeof(IEnumerable<>),
        typeof(IEnumerator<>),
        typeof(IEqualityComparer<>),
        typeof(IList<>),
        typeof(IReadOnlyList<>),
        typeof(KeyNotFoundException),
        typeof(KeyValuePair<,>),
        typeof(List<>),

        // mscorlib System.Collections.ObjectModel

        typeof(Collection<>),
        typeof(KeyedCollection<,>),
        typeof(ReadOnlyCollection<>),

        // System System.Collections.Generic

        typeof(LinkedList<>),
        typeof(LinkedListNode<>),
        typeof(Queue<>),
        typeof(SortedDictionary<,>),
        typeof(SortedList<,>),
        typeof(Stack<>),

        // System System.Collections.Specialized

        typeof(BitVector32),

        // System.Core System.Collections.Generic

        typeof(HashSet<>),

        // System.Core System.Linq

        typeof(Enumerable),
        typeof(IGrouping<,>),
        typeof(ILookup<,>),
        typeof(IOrderedEnumerable<>),
        typeof(IOrderedQueryable),
        typeof(IOrderedQueryable<>),
        typeof(IQueryable),
        typeof(IQueryable<>),
        typeof(IQueryProvider),
        typeof(Lookup<,>),
        typeof(Queryable),
    };

    /// <summary>
    /// These types probably shouldn't be exposed, because they allow filesystem access,
    /// or because they provide more advanced functionality that mods shouldn't depend on.
    /// Proceed with caution.
    /// </summary>
    private static Type[] QuestionableTypes = new Type[] {

        //// mscorlib System

        //typeof(System.AsyncCallback),
        //typeof(System.BitConverter),
        //typeof(System.Buffer),
        //typeof(System.DateTime),
        //typeof(System.DateTimeKind),
        //typeof(System.DateTimeOffset),
        //typeof(System.DayOfWeek),
        //typeof(System.EventArgs),
        //typeof(System.EventHandler),
        //typeof(System.EventHandler<>),
        //typeof(System.TimeSpan),
        //typeof(System.TimeZone),
        //typeof(System.TimeZoneInfo),
        //typeof(System.TimeZoneNotFoundException),

        //// mscorlib System.IO

        //typeof(System.IO.BinaryReader),
        //typeof(System.IO.BinaryWriter),
        //typeof(System.IO.BufferedStream),
        //typeof(System.IO.EndOfStreamException),
        //typeof(System.IO.FileAccess),
        //typeof(System.IO.FileMode),
        //typeof(System.IO.FileNotFoundException),
        //typeof(System.IO.IOException),
        //typeof(System.IO.MemoryStream),
        //typeof(System.IO.Path),
        //typeof(System.IO.PathTooLongException),
        //typeof(System.IO.SeekOrigin),
        //typeof(System.IO.Stream),
        //typeof(System.IO.StringReader),
        //typeof(System.IO.StringWriter),
        //typeof(System.IO.TextReader),
        //typeof(System.IO.TextWriter),

        //// mscorlib System.Text

        //typeof(System.Text.ASCIIEncoding),
        //typeof(System.Text.Decoder),
        //typeof(System.Text.Encoder),
        //typeof(System.Text.Encoding),
        //typeof(System.Text.EncodingInfo),
        //typeof(System.Text.StringBuilder),
        //typeof(System.Text.UnicodeEncoding),
        //typeof(System.Text.UTF32Encoding),
        //typeof(System.Text.UTF7Encoding),
        //typeof(System.Text.UTF8Encoding),

        //// mscorlib System.Globalization

        //typeof(System.Globalization.CharUnicodeInfo),
        //typeof(System.Globalization.CultureInfo),
        //typeof(System.Globalization.DateTimeFormatInfo),
        //typeof(System.Globalization.DateTimeStyles),
        //typeof(System.Globalization.NumberFormatInfo),
        //typeof(System.Globalization.NumberStyles),
        //typeof(System.Globalization.RegionInfo),
        //typeof(System.Globalization.StringInfo),
        //typeof(System.Globalization.TextElementEnumerator),
        //typeof(System.Globalization.TextInfo),
        //typeof(System.Globalization.UnicodeCategory),

        //// System System.IO.Compression

        //typeof(System.IO.Compression.CompressionMode),
        //typeof(System.IO.Compression.DeflateStream),
        //typeof(System.IO.Compression.GZipStream),

        //// System System.Text.RegularExpressions

        //typeof(System.Text.RegularExpressions.Capture),
        //typeof(System.Text.RegularExpressions.CaptureCollection),
        //typeof(System.Text.RegularExpressions.Group),
        //typeof(System.Text.RegularExpressions.GroupCollection),
        //typeof(System.Text.RegularExpressions.Match),
        //typeof(System.Text.RegularExpressions.MatchCollection),
        //typeof(System.Text.RegularExpressions.MatchEvaluator),
        //typeof(System.Text.RegularExpressions.Regex),
        //typeof(System.Text.RegularExpressions.RegexCompilationInfo),
        //typeof(System.Text.RegularExpressions.RegexOptions),

    };
    #endregion

    private class CompilerWrapperReportPrinter : ReportPrinter
    {
        private readonly ReportPrinter fullReportPrinter;
        private readonly StringBuilder fullReportStringBuilder;

        private StringBuilder partialReportStringBuilder;
        private ReportPrinter partialReportPrinter;

        public string FullReport => fullReportStringBuilder.ToString();
        public int FullReportErrorCount => fullReportPrinter.ErrorsCount;
        public int FullReportWarningCount => fullReportPrinter.WarningsCount;

        public string PartialReport => partialReportStringBuilder.ToString();
        public int PartialReportErrorCount => partialReportPrinter.ErrorsCount;
        public int PartialReportWarningCount => partialReportPrinter.WarningsCount;

        public CompilerWrapperReportPrinter()
        {
            fullReportStringBuilder = new StringBuilder();
            fullReportPrinter = new StreamReportPrinter(new StringWriter(fullReportStringBuilder));

            StartNewPartialReport();
        }

        public override void Print(AbstractMessage msg, bool showFullPath)
        {
            fullReportPrinter.Print(msg, showFullPath);
            partialReportPrinter.Print(msg, showFullPath);
        }

        public void StartNewPartialReport()
        {
            partialReportStringBuilder = new StringBuilder();
            partialReportPrinter = new StreamReportPrinter(new StringWriter(partialReportStringBuilder));
        }
    }
}
