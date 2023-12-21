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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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

    public CompilerWrapper()
    {
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
        };
        this.reportPrinter = new CompilerWrapperReportPrinter();
        this.context = new CompilerContext(settings, reportPrinter);

        this.evaluator = new Evaluator(context);

        evaluator.ImportTypes(true, BuiltInTypes);
        evaluator.ImportTypes(true, AsynchronousFunctionSupportTypes);
        evaluator.ImportTypes(true, AdditionalTypes);
        evaluator.ImportTypes(true, QuestionableTypes);
    }

    public void ImportTypes(Type[] types)
    {
        this.evaluator.ImportTypes(true, types);
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

    public object EvaluateExpression(string code, out object result, out bool isResultSet)
    {
        return evaluator.Evaluate(code, out result, out isResultSet);
    }

    public void StartNewPartialReport()
    {
        reportPrinter.StartNewPartialReport();
    }

    #region Allowed Types

    /// <summary>
    /// Basic built-in system types.
    /// </summary>
    private static Type[] BuiltInTypes = new Type[]
    {
        typeof(Array),
        typeof(Attribute),
        // typeof(BinaryPromotionsTypes),
        typeof(bool),
        typeof(byte),
        typeof(char),
        typeof(decimal),
        typeof(Delegate),
        typeof(double),
        // typeof(Dynamic),
        typeof(Enum),
        typeof(Exception),
        typeof(float),
        typeof(IDisposable),
        typeof(IEnumerable),
        typeof(IEnumerator),
        typeof(int),
        typeof(IntPtr),
        typeof(long),
        typeof(MulticastDelegate),
        typeof(object),
        typeof(ParamArrayAttribute),
        // typeof(PredefinedOperator[] OperatorsBinaryEquality),
        // typeof(PredefinedOperator[] OperatorsBinaryStandard),
        // typeof(PredefinedOperator[] OperatorsBinaryUnsafe),
        // typeof(OperatorsUnary),
        // typeof(OperatorsUnaryMutator),
        typeof(OutAttribute),
        typeof(RuntimeFieldHandle),
        typeof(RuntimeTypeHandle),
        typeof(sbyte),
        typeof(short),
        typeof(string),
        typeof(Type),
        typeof(uint),
        typeof(UIntPtr),
        typeof(ulong),
        typeof(ushort),
        typeof(ValueType),
        typeof(void),
    };

    private static Type[] AsynchronousFunctionSupportTypes = new Type[]
    {
        // https://stackoverflow.com/questions/17969603/what-is-the-minimum-set-of-types-required-to-compile-async-code#17969731
        typeof(IAsyncStateMachine),
        typeof(INotifyCompletion),
        typeof(ICriticalNotifyCompletion),
        typeof(AsyncVoidMethodBuilder),
        typeof(AsyncTaskMethodBuilder), typeof(AsyncTaskMethodBuilder<>),
        typeof(AsyncValueTaskMethodBuilder), typeof(AsyncValueTaskMethodBuilder<>),
        typeof(Task),
        typeof(Task<>),
    };

    /// <summary>
    /// These types may be useful in scripts but they're not strictly necessary and
    /// should be edited as desired.
    /// </summary>
    private static Type[] AdditionalTypes = new Type[]
    {
        // mscorlib System

        typeof(Action), typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>),
        typeof(ArgumentException), typeof(ArgumentNullException), typeof(ArgumentOutOfRangeException),
        typeof(ArithmeticException), typeof(ArraySegment<>), typeof(ArrayTypeMismatchException), typeof(Comparison<>),
        typeof(Convert), typeof(Converter<,>), typeof(DivideByZeroException), typeof(FlagsAttribute),
        typeof(FormatException), typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>),
        typeof(Func<,,,,>), typeof(Guid), typeof(IAsyncResult), typeof(ICloneable), typeof(IComparable),
        typeof(IComparable<>), typeof(IConvertible), typeof(ICustomFormatter), typeof(IEquatable<>),
        typeof(IFormatProvider), typeof(IFormattable), typeof(IObservable<>), typeof(IndexOutOfRangeException),
        typeof(InvalidCastException), typeof(InvalidOperationException), typeof(InvalidTimeZoneException), typeof(Math),
        typeof(MidpointRounding), typeof(NonSerializedAttribute), typeof(NotFiniteNumberException),
        typeof(NotImplementedException), typeof(NotSupportedException), typeof(Nullable), typeof(Nullable<>),
        typeof(NullReferenceException), typeof(ObjectDisposedException), typeof(ObsoleteAttribute),
        typeof(OverflowException), typeof(Predicate<>), typeof(Random), typeof(RankException),
        typeof(SerializableAttribute), typeof(StackOverflowException), typeof(StringComparer), typeof(StringComparison),
        typeof(StringSplitOptions), typeof(SystemException), typeof(TimeoutException), typeof(TypeCode),
        typeof(Version), typeof(WeakReference),

        // mscorlib System.Collections

        typeof(BitArray), typeof(ICollection), typeof(IComparer), typeof(IDictionary), typeof(IDictionaryEnumerator),
        typeof(IEqualityComparer), typeof(IList),

        // mscorlib System.Collections.Generic

        typeof(Comparer<>), typeof(Dictionary<,>), typeof(EqualityComparer<>),
        typeof(ICollection<>), typeof(IComparer<>), typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>),
        typeof(IEnumerable<>), typeof(IEnumerator<>), typeof(IEqualityComparer<>), typeof(IList<>), typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>), typeof(KeyNotFoundException), typeof(KeyValuePair<,>), typeof(List<>),

        // mscorlib System.Collections.ObjectModel

        typeof(Collection<>), typeof(KeyedCollection<,>), typeof(ReadOnlyCollection<>),

        // System System.Collections.Generic

        typeof(LinkedList<>), typeof(LinkedListNode<>), typeof(Queue<>), typeof(SortedDictionary<,>),
        typeof(SortedList<,>), typeof(Stack<>),

        // System System.Collections.Specialized

        typeof(BitVector32),

        // System.Core System.Collections.Generic

        typeof(HashSet<>),

        // System.Core System.Linq

        typeof(Enumerable), typeof(IGrouping<,>), typeof(ILookup<,>), typeof(IOrderedEnumerable<>),
        typeof(IOrderedQueryable), typeof(IOrderedQueryable<>), typeof(IQueryable), typeof(IQueryable<>),
        typeof(IQueryProvider), typeof(Lookup<,>), typeof(Queryable),

        // System.Xml
        typeof(XDocument), typeof(XElement),

        // System.Xml.Linq
        typeof (Extensions),

        // System.Xml.XPath
        typeof (System.Xml.XPath.Extensions),
    };

    /// <summary>
    /// These types probably shouldn't be exposed, because they allow filesystem access,
    /// or because they provide more advanced functionality that mods shouldn't depend on.
    /// Proceed with caution.
    /// </summary>
    private static Type[] QuestionableTypes = new Type[]
    {
        //// mscorlib System

        //typeof(System.AsyncCallback),
        typeof(BitConverter),
        typeof(Buffer),
        typeof(DateTime),
        typeof(DateTimeKind),
        typeof(DateTimeOffset),
        typeof(DayOfWeek),
        //typeof(System.EventArgs),
        //typeof(System.EventHandler),
        //typeof(System.EventHandler<>),
        typeof(TimeSpan),
        typeof(TimeZone),
        typeof(TimeZoneInfo),
        typeof(TimeZoneNotFoundException),

        //// mscorlib System.IO

        typeof(BinaryReader),
        typeof(BinaryWriter),
        typeof(BufferedStream),
        typeof(EndOfStreamException),
        typeof(Directory),
        typeof(File),
        typeof(FileAccess),
        typeof(FileMode),
        typeof(FileNotFoundException),
        typeof(FileStream),
        typeof(IOException),
        typeof(MemoryStream),
        typeof(Path),
        typeof(PathTooLongException),
        typeof(SeekOrigin),
        typeof(Stream),
        typeof(StringReader),
        typeof(StringWriter),
        typeof(TextReader),
        typeof(TextWriter),

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

        //// mscorlib System.Text

        typeof(ASCIIEncoding),
        typeof(Decoder),
        typeof(Encoder),
        typeof(Encoding),
        typeof(EncodingInfo),
        typeof(StringBuilder),
        typeof(UnicodeEncoding),
        typeof(UTF32Encoding),
        typeof(UTF7Encoding),
        typeof(UTF8Encoding),

        //// System System.Text.RegularExpressions

        typeof(Capture),
        typeof(CaptureCollection),
        typeof(Group),
        typeof(GroupCollection),
        typeof(Match),
        typeof(MatchCollection),
        typeof(MatchEvaluator),
        typeof(Regex),
        typeof(RegexCompilationInfo),
        typeof(RegexOptions),

        //// System.Threading

        typeof(CancellationTokenSource),
        typeof(CancellationToken),
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
