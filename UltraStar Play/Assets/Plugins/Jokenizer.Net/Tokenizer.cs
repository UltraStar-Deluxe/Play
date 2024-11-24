using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Jokenizer.Net {
    using Tokens;

    public class Tokenizer {
        private static string separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        protected readonly Settings settings;
        protected readonly string exp;
        private readonly int len;
        private int _idx;
        protected int idx => _idx;
        private char _ch;
        protected char ch => _ch;

        public Tokenizer(string exp, Settings settings = null) {
            if (exp == null)
                throw new ArgumentNullException(nameof(exp));
            if (string.IsNullOrWhiteSpace(exp))
                throw new ArgumentException(nameof(exp));

            this.settings = settings ?? Settings.Default;
            this.exp = exp;

            this.len = exp.Length;
            _ch = exp.ElementAt(0);
        }

        public virtual Token GetToken() {
            Skip();

            var t = TryLiteral()
                ?? TryVariable()
                ?? TryParameter()
                ?? TryUnary()
                ?? TryGroup()
                ?? TryObject()
                ?? (Token)TryShortArray();

            if (t == null) return t;

            if (t is VariableToken vt) {
                if (settings.TryGetKnownValue(vt.Name, out var value)) {
                    t = new LiteralToken(value);
                } else if (vt.Name == "new") {
                    Skip();
                    t = (Token)TryObject() ?? GetArray();
                }
            }

            if (Done()) return t;

            Token r;
            do {
                Skip();

                r = t;
                t = TryMember(t)
                    ?? TryIndexer(t)
                    ?? TryLambda(t)
                    ?? TryCall(t)
                    ?? TryTernary(t)
                    ?? (Token)TryBinary(t);
            } while (t != null);

            return r;
        }

        protected virtual LiteralToken TryNumber() {
            var n = GetNumber();

            bool isFloat = false;
            if (Get(separator)) {
                n += separator;
                n += GetNumber();
                isFloat = true;
            }

            if (n != "") {
                if (IsVariableStart())
                    throw new InvalidSyntaxException($"Unexpected character (${ch}) at index ${idx}");

                var val = isFloat ? float.Parse(n) : Convert.ChangeType(int.Parse(n), typeof(int));
                return new LiteralToken(val);
            }

            return null;
        }

        protected string GetNumber() {
            var n = "";
            while (IsNumber()) {
                n += ch;
                Move();
            }
            return n;
        }

        protected virtual Token TryString() {
            bool inter = false;
            if (ch == '$') {
                inter = true;
                Move();
            }
            if (ch != '"') return null;

            var q = ch;
            var es = new List<Token>();
            var s = "";

            for (char c = Move(); !Done(); c = Move()) {
                if (c == q) {
                    Move();

                    if (es.Count > 0) {
                        if (s != "") {
                            es.Add(new LiteralToken(s));
                        }

                        return es.Aggregate(
                            (Token)new LiteralToken(""),
                            (p, n) => new BinaryToken("+", p, n)
                        );
                    }

                    return new LiteralToken(s);
                }

                if (c == '\\') {
                    c = Move();
                    switch (c) {
                        case 'a':
                            s += '\a';
                            break;
                        case 'b':
                            s += '\b';
                            break;
                        case 'f':
                            s += '\f';
                            break;
                        case 'n':
                            s += '\n';
                            break;
                        case 'r':
                            s += '\r';
                            break;
                        case 't':
                            s += '\t';
                            break;
                        case 'v':
                            s += '\v';
                            break;
                        case '0':
                            s += '\0';
                            break;
                        case '\\':
                            s += '\\';
                            break;
                        case '"':
                            s += '"';
                            break;
                        default:
                            s += "\\" + c;
                            break;
                    }
                } else if (inter && Get("{")) {
                    if (s != "") {
                        es.Add(new LiteralToken(s));
                        s = "";
                    }
                    es.Add(GetToken());

                    Skip();
                    if (ch != '}')
                        throw new InvalidSyntaxException($"Unterminated template literal at {idx}");
                } else {
                    s += c;
                }
            }

            throw new InvalidSyntaxException($"Unclosed quote after {s}");
        }

        protected virtual Token TryLiteral() {
            return TryNumber() ?? TryString();
        }

        protected virtual VariableToken TryVariable() {
            var v = GetVariableName();
            return v != "" ? new VariableToken(v) : null;
        }

        protected string GetVariableName() {
            var v = "";

            if (IsVariableStart()) {
                do {
                    v += ch;
                    Move();
                } while (StillVariable());
            }

            return v;
        }

        protected virtual VariableToken TryParameter() {
            if (!Get("@")) return null;

            var n = GetNumber();
            if (string.IsNullOrEmpty(n))
                throw new InvalidSyntaxException($"Invalid parameter at {idx}");

            return new VariableToken("@" + n);
        }

        protected virtual UnaryToken TryUnary() {
            if (settings.ContainsUnary(ch)) {
                var u = ch;
                Move();
                return new UnaryToken(u, GetToken());
            }

            return null;
        }

        protected virtual GroupToken TryGroup() {
            return Get("(") ? new GroupToken(GetGroup()) : null;
        }

        protected virtual IEnumerable<Token> GetGroup() {
            var es = new List<Token>();
            do {
                var e = GetToken();
                if (e != null) {
                    es.Add(e);
                }
            } while (Get(","));

            To(")");

            return es;
        }

        protected virtual ObjectToken TryObject() {
            if (!Get("{")) return null;

            var es = new List<AssignToken>();
            do {
                Skip();
                var member = GetToken();
                if (!(member is IVariableToken vt))
                    throw new InvalidSyntaxException($"Invalid assignment at {idx}");

                Skip();
                if (Get("=")) {
                    if (member.GetType() != typeof(VariableToken))
                        throw new InvalidSyntaxException($"Invalid assignment at {idx}");

                    Skip();

                    es.Add(new AssignToken(vt.Name, GetToken()));
                } else {
                    es.Add(new AssignToken(vt.Name, member));
                }
            } while (Get(","));

            To("}");

            return new ObjectToken(es);
        }

        protected virtual ArrayToken TryShortArray() {
            if (!Get("[")) return null;

            var tokens = GetArrayTokens();
            To("]");

            return new ArrayToken(tokens);
        }

        protected virtual ArrayToken GetArray() {
            To("[");
            To("]");
            To("{");
            var tokens = GetArrayTokens();
            To("}");

            return new ArrayToken(tokens);
        }

        protected IEnumerable<Token> GetArrayTokens() {
            var ts = new List<Token>();
            do {
                Skip();
                var token = GetToken();
                if (token == null) {
                    if (ts.Count > 0)
                        throw new InvalidSyntaxException($"Invalid array item at {idx}");

                    break;
                }

                ts.Add(token);
            } while (Get(","));

            return ts;
        }

        protected virtual MemberToken TryMember(Token t) {
            if (!Get(".")) return null;

            Skip();
            var v = GetVariableName();
            if (string.IsNullOrEmpty(v)) throw new InvalidSyntaxException($"Invalid member identifier at {idx}");

            return new MemberToken(t, v);
        }

        protected virtual IndexerToken TryIndexer(Token t) {
            if (!Get("[")) return null;

            Skip();
            var k = GetToken();
            To("]");

            return new IndexerToken(t, k);
        }

        protected virtual LambdaToken TryLambda(Token t) {
            if (!Get("=>"))
                return null;

            return new LambdaToken(GetToken(), GetParameters(t));
        }

        protected IEnumerable<string> GetParameters(Token t) {
            if (t is GroupToken gt) {
                return gt.Tokens.Select(x => {
                    if (!(x is IVariableToken xv))
                        throw new InvalidSyntaxException($"Invalid parameter at {idx}");

                    return xv.Name;
                });
            }

            if (!(t is IVariableToken vt))
                throw new InvalidSyntaxException($"Invalid parameter at {idx}");

            return new[] { vt.Name };
        }

        protected virtual CallToken TryCall(Token t) {
            return Get("(") ? GetCall(t) : null;
        }

        protected CallToken GetCall(Token t) {
            var args = GetGroup();

            return new CallToken(t, args);
        }

        protected virtual TernaryToken TryTernary(Token t) {
            if (!Get("?")) return null;

            var whenTrue = GetToken();
            To(":");
            var whenFalse = GetToken();

            return new TernaryToken(t, whenTrue, whenFalse);
        }

        protected virtual BinaryToken TryBinary(Token t) {
            var op = settings.BinaryOperators.FirstOrDefault(b => Get(b));
            if (op == null) return null;

            var right = GetToken();

            if (right is BinaryToken bt)
                return FixPrecedence(t, op, bt);

            return new BinaryToken(op, t, right);
        }

        protected bool IsSpace() {
            return Char.IsWhiteSpace(ch);
        }

        protected bool IsNumber() {
            return char.IsNumber(ch);
        }

        protected bool IsVariableStart() {
            return ch == 95                 // `_`
                || (ch >= 65 && ch <= 90)   // A...Z
                || (ch >= 97 && ch <= 122); // a...z
        }

        protected bool StillVariable() {
            return IsVariableStart() || IsNumber();
        }

        protected bool Done() {
            return idx >= len;
        }

        protected char Move(int count = 1) {
            _idx += count;
            var d = Done();
            return _ch = d ? '\0' : exp.ElementAt(idx);
        }

        protected bool Get(string s) {
            if (Eq(idx, s)) {
                Move(s.Length);
                return true;
            }

            return false;
        }

        protected void Skip() {
            while (IsSpace()) Move();
        }

        protected bool Eq(int index, string target) {
            if (index + target.Length > exp.Length) return false;
            return exp.Substring(idx, target.Length) == target;
        }

        protected void To(string c) {
            Skip();

            if (!Eq(idx, c))
                throw new InvalidSyntaxException($"Expected {c} at index {idx}, found {ch}");

            Move(c.Length);
        }

        protected BinaryToken FixPrecedence(Token left, string leftOp, BinaryToken right) {
            settings.TryGetBinaryInfo(leftOp, out var lo);
            settings.TryGetBinaryInfo(right.Operator, out var ro);

            return ro.Precedence < lo.Precedence
                ? new BinaryToken(right.Operator, new BinaryToken(leftOp, left, right.Left), right.Right)
                : new BinaryToken(leftOp, left, right);
        }

        public static Token Parse(string exp, Settings settings = null) {
            return new Tokenizer(exp, settings).GetToken();
        }

        public static T Parse<T>(string exp, Settings settings = null) where T : Token {
            return (T)Parse(exp, settings);
        }
    }
}
