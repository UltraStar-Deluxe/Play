using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net {
    using Dynamic;
    using Tokens;

    public class TokenVisitor {
        private static readonly MethodInfo concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

        protected readonly Settings settings;
        protected readonly IDictionary<string, object> variables;

        public TokenVisitor(IDictionary<string, object> variables, IEnumerable<object> values, Settings settings = null) {
            this.variables = variables ?? new Dictionary<string, object>();
            this.settings = settings ?? Settings.Default;

            if (values != null) {
                var i = 0;
                values.ToList().ForEach(e => {
                    var k = $"@{i++}";
                    if (!this.variables.ContainsKey(k)) {
                        this.variables.Add(k, e);
                    }
                });
            }
        }

        public virtual LambdaExpression Process(Token token, IEnumerable<Type> typeParameters, IEnumerable<ParameterExpression> parameters = null) {
            typeParameters = typeParameters ?? Enumerable.Empty<Type>();
            parameters = parameters ?? Enumerable.Empty<ParameterExpression>();

            if (token is LambdaToken lt)
                return VisitLambda(lt, typeParameters, parameters);

            var prms = typeParameters.Select(Expression.Parameter).ToList();
            var body = Visit(token, parameters.Concat(prms).ToList());

            return Expression.Lambda(body, prms);
        }

        public virtual Expression Visit(Token token, IEnumerable<ParameterExpression> parameters) {
            if (token is GroupToken gt && gt.Tokens.Length == 1)
                return Visit(gt.Tokens[0], parameters);

            switch (token) {
                case BinaryToken bt:
                    return VisitBinary(bt, parameters);
                case CallToken ct:
                    return VisitCall(ct, parameters);
                case IndexerToken it:
                    return VisitIndexer(it, parameters);
                case LiteralToken lit:
                    return VisitLiteral(lit, parameters);
                case MemberToken mt:
                    return VisitMember(mt, parameters);
                case ObjectToken ot:
                    return VisitObject(ot, parameters);
                case ArrayToken at:
                    return VisitArray(at, parameters);
                case TernaryToken tt:
                    return VisitTernary(tt, parameters);
                case UnaryToken ut:
                    return VisitUnary(ut, parameters);
                case VariableToken vt:
                    return VisitVariable(vt, parameters);
                case GroupToken gt2:
                case AssignToken at:
                case LambdaToken lt:
                    throw new InvalidTokenException($"Invalid {token.Type} expression usage");
                default:
                    throw new InvalidTokenException($"Unsupported token type {token.Type}");
            }
        }

        protected virtual Expression VisitBinary(BinaryToken token, IEnumerable<ParameterExpression> parameters) {
            var left = Visit(token.Left, parameters);
            var right = Visit(token.Right, parameters);

            if (left.Type == typeof(string) && token.Operator == "+")
                return Expression.Add(left, right, concatMethod);

            return GetBinary(token.Operator, left, right);
        }

        protected virtual Expression VisitCall(CallToken token, IEnumerable<ParameterExpression> parameters) {
            Expression owner;
            string methodName;

            if (token.Callee is MemberToken mt) {
                owner = Visit(mt.Owner, parameters);
                methodName = mt.Name;
            } else if (token.Callee is VariableToken vt && parameters.Count() == 1) {
                owner = parameters.First();
                methodName = vt.Name;
            } else
                throw new InvalidTokenException("Unsupported method call");

            return GetMethodCall(owner, methodName, token.Args, parameters);
        }

        protected virtual Expression VisitIndexer(IndexerToken token, IEnumerable<ParameterExpression> parameters) {
            var owner = Visit(token.Owner, parameters);
            var key = Visit(token.Key, parameters);

            return CreateIndexer(owner, key);
        }

        protected Expression CreateIndexer(Expression owner, Expression key) {
            if (owner.Type.IsArray && key.Type == typeof(int))
                return Expression.ArrayIndex(owner, key);

            PropertyInfo indexer;
            if (owner.Type == typeof(ExpandoObject)) {
                owner = Expression.Convert(owner, typeof(IDictionary<string, object>));
                indexer = owner.Type.GetProperty("Item");
            } else {
                var defaultMemberAttr = (DefaultMemberAttribute)owner.Type.GetCustomAttribute(typeof(DefaultMemberAttribute));
                indexer = owner.Type.GetProperty(defaultMemberAttr?.MemberName ?? "Item");
            }

            if (indexer == null)
                throw new InvalidTokenException($"Cannot find indexer on type {owner.Type}");

            return Expression.MakeIndex(owner, indexer, new[] { key });
        }

        protected virtual LambdaExpression VisitLambda(LambdaToken token, IEnumerable<Type> typeParameters, IEnumerable<ParameterExpression> parameters = null) {
            var prms = typeParameters.Zip(token.Parameters, Expression.Parameter).ToList();
            var body = Visit(token.Body, parameters.Concat(prms).ToList());

            return Expression.Lambda(body, prms);
        }

        protected virtual Expression VisitLiteral(LiteralToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.Constant(token.Value, token.Value != null ? token.Value.GetType() : typeof(object));
        }

        protected virtual Expression VisitMember(MemberToken token, IEnumerable<ParameterExpression> parameters) {
            var owner = Visit(token.Owner, parameters);
            return GetMember(owner, token.Name, parameters);
        }

        protected Expression GetMember(Expression owner, string name, IEnumerable<ParameterExpression> parameters) {
            var prop = owner.Type.GetProperty(name);
            if (prop != null)
                return Expression.Property(owner, prop);

            var field = owner.Type.GetField(name);
            if (field != null)
                return Expression.Field(owner, field);

            return CreateIndexer(owner, Expression.Constant(name));
        }

        protected virtual Expression VisitObject(ObjectToken token, IEnumerable<ParameterExpression> parameters) {
            var props = token.Members.Select(m => new { m.Name, Right = Visit(m.Right, parameters) });
            var type = ClassFactory.Instance.GetDynamicClass(props.Select(p => new DynamicProperty(p.Name, p.Right.Type)));
            var newExp = Expression.New(type.GetConstructors().First());
            var bindings = props.Select(p => Expression.Bind(type.GetProperty(p.Name), p.Right));

            return Expression.MemberInit(newExp, bindings);
        }

        protected virtual Expression VisitArray(ArrayToken token, IEnumerable<ParameterExpression> parameters) {
            var expressions = token.Items.Select(i => Visit(i, parameters)).ToList();
            var type = expressions.Any() ? expressions[0].Type : typeof(object);
            return Expression.NewArrayInit(type, expressions);
        }

        protected virtual Expression VisitTernary(TernaryToken token, IEnumerable<ParameterExpression> parameters) {
            return Expression.Condition(Visit(token.Predicate, parameters), Visit(token.WhenTrue, parameters), Visit(token.WhenFalse, parameters));
        }

        protected virtual Expression VisitUnary(UnaryToken token, IEnumerable<ParameterExpression> parameters) {
            return GetUnary(token.Operator, Visit(token.Target, parameters));
        }

        protected virtual Expression VisitVariable(VariableToken token, IEnumerable<ParameterExpression> parameters) {
            var name = token.Name;

            if (this.variables.TryGetValue(name, out var value))
                return Expression.Constant(value, value != null ? value.GetType() : typeof(object));

            var prm = parameters.FirstOrDefault(p => p.Name == name);
            if (prm != null)
                return prm;

            if (name == "Math")
                return Expression.Parameter(typeof(Math));

            if (parameters.Count() == 1) {
                var owner = parameters.First();
                return GetMember(owner, name, parameters);
            }

            throw new InvalidTokenException($"Unknown variable {name}");
        }

        protected MethodCallExpression GetMethodCall(Expression owner, string methodName, Token[] args, IEnumerable<ParameterExpression> parameters) {
            if (methodName == "GetType")
                throw new InvalidOperationException("GetType cannot be called");

            var methodArgs = new Expression[args.Length];
            var lambdaArgs = new Dictionary<int, LambdaToken>();
            for (var i = 0; i < args.Length; i++) {
                var arg = args[i];
                if (arg is LambdaToken lt) {
                    lambdaArgs.Add(i, lt);
                } else {
                    methodArgs[i] = Visit(arg, parameters);
                }
            }

            MethodInfo method;
            ParameterInfo[] methodPrms = null;
            var isExtension = false;
            if (!lambdaArgs.Any()) {
                method = owner.Type.GetMethod(methodName, methodArgs.Select(m => m.Type).ToArray());
                if (method != null) {
                    methodPrms = method.GetParameters();
                    // we might need to cast to target types
                    for (var i = 0; i < methodArgs.Length; i++) {
                        var prm = methodPrms[i];
                        var arg = methodArgs[i];
                        if (arg != null && arg.Type != prm.ParameterType) {
                            methodArgs[i] = Expression.Convert(arg, prm.ParameterType);
                        }
                    }
                }
            } else {
                method = owner.Type.GetMethods()
                    .FirstOrDefault(m => m.Name == methodName && Helper.IsSuitable(m.GetParameters(), methodArgs));
            }
            if (method == null) {
                isExtension = true;
                method = ExtensionMethods.Find(owner.Type, methodName, methodArgs);
            }

            if (method == null)
                throw new InvalidTokenException($"Could not find instance or extension method for {methodName} for {owner.Type}");

            methodPrms = methodPrms ?? method.GetParameters();

            foreach (var lambdaArg in lambdaArgs) {
                var g = methodPrms[lambdaArg.Key].ParameterType.GetGenericArguments();
                methodArgs[lambdaArg.Key] = VisitLambda(lambdaArg.Value, g, parameters);
            }

            return isExtension
                ? Expression.Call(null, method, new[] { owner }.Concat(methodArgs))
                : Expression.Call(method.IsStatic ? null : owner, method, methodArgs);
        }

        protected Expression GetBinary(string op, Expression left, Expression right) {
            if (settings.TryGetBinaryInfo(op, out var bi))
                return bi.ExpressionConverter(left, right);

            throw new InvalidTokenException($"Unknown binary operator {op}");
        }

        protected Expression GetUnary(char op, Expression exp) {
            if (settings.TryGetUnaryConverter(op, out var uc))
                return uc(exp);

            throw new InvalidTokenException($"Unknown unary operator {op}");
        }
    }
}
