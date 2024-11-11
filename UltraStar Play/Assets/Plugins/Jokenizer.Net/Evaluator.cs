using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Jokenizer.Net {
    using Tokens;

    public static class Evaluator {

        #region Lambda

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => new TokenVisitor(variables, parameters, settings).Process(token, typeParameters);

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda(token, typeParameters, variables, null, parameters);

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, Settings settings, params object[] parameters)
            => ToLambda(token, typeParameters, null, settings, parameters);

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, params object[] parameters)
            => ToLambda(token, typeParameters, (Settings)null, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => (Expression<Func<TResult>>)ToLambda(token, null, variables, settings, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda<TResult>(token, variables, null, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, Settings settings, params object[] parameters)
            => ToLambda<TResult>(token, null, settings, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, params object[] parameters)
            => ToLambda<TResult>(token, (Settings)null, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => (Expression<Func<T, TResult>>)ToLambda(token, new[] { typeof(T) }, variables, settings, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda<T, TResult>(token, variables, null, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, Settings settings, params object[] parameters)
            => ToLambda<T, TResult>(token, null, settings, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, params object[] parameters)
            => ToLambda<T, TResult>(token, (Settings)null, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => (Expression<Func<T1, T2, TResult>>)ToLambda(token, new[] { typeof(T1), typeof(T2) }, variables, settings, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda<T1, T2, TResult>(token, variables, null, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, Settings settings, params object[] parameters)
            => ToLambda<T1, T2, TResult>(token, null, settings, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, params object[] parameters)
            => ToLambda<T1, T2, TResult>(token, (Settings)null, parameters);

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToLambda(Tokenizer.Parse(token, settings), typeParameters, variables, settings, parameters);

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda(token, typeParameters, variables, null, parameters);

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, Settings settings, params object[] parameters)
            => ToLambda(token, typeParameters, null, settings, parameters);

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, params object[] parameters)
            => ToLambda(token, typeParameters, (Settings)null, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(string token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToLambda<TResult>(Tokenizer.Parse(token, settings), variables, settings, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(string token, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda<TResult>(token, variables, null, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(string token, Settings settings, params object[] parameters)
            => ToLambda<TResult>(token, null, settings, parameters);

        public static Expression<Func<TResult>> ToLambda<TResult>(string token, params object[] parameters)
            => ToLambda<TResult>(token, (Settings)null, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(string token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToLambda<T, TResult>(Tokenizer.Parse(token, settings), variables, settings, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(string token, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda<T, TResult>(token, variables, null, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(string token, Settings settings, params object[] parameters)
            => ToLambda<T, TResult>(token, null, settings, parameters);

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(string token, params object[] parameters)
            => ToLambda<T, TResult>(token, (Settings)null, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(string token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToLambda<T1, T2, TResult>(Tokenizer.Parse(token, settings), variables, settings, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(string token, IDictionary<string, object> variables, params object[] parameters)
            => ToLambda<T1, T2, TResult>(token, variables, null, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(string token, Settings settings, params object[] parameters)
            => ToLambda<T1, T2, TResult>(token, null, settings, parameters);

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(string token, params object[] parameters)
            => ToLambda<T1, T2, TResult>(token, (Settings)null, parameters);

        #endregion

        #region Func

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToLambda(token, typeParameters, variables, settings, parameters).Compile();

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc(token, typeParameters, variables, null, parameters);

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, Settings settings, params object[] parameters)
            => ToFunc(token, typeParameters, null, settings, parameters);

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, params object[] parameters)
            => ToFunc(token, typeParameters, (Settings)null, parameters);

        public static Func<TResult> ToFunc<TResult>(Token token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => (Func<TResult>)ToFunc(token, null, variables, settings, parameters);

        public static Func<TResult> ToFunc<TResult>(Token token, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc<TResult>(token, variables, null, parameters);

        public static Func<TResult> ToFunc<TResult>(Token token, Settings settings, params object[] parameters)
            => ToFunc<TResult>(token, null, settings, parameters);

        public static Func<TResult> ToFunc<TResult>(Token token, params object[] parameters)
            => ToFunc<TResult>(token, (Settings)null, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(Token token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => (Func<T, TResult>)ToFunc(token, new[] { typeof(T) }, variables, settings, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(Token token, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc<T, TResult>(token, variables, null, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(Token token, Settings settings, params object[] parameters)
            => ToFunc<T, TResult>(token, null, settings, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(Token token, params object[] parameters)
            => ToFunc<T, TResult>(token, (Settings)null, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(Token token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => (Func<T1, T2, TResult>)ToFunc(token, new[] { typeof(T1), typeof(T2) }, variables, settings, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(Token token, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc<T1, T2, TResult>(token, variables, null, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(Token token, Settings settings, params object[] parameters)
            => ToFunc<T1, T2, TResult>(token, null, settings, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(Token token, params object[] parameters)
            => ToFunc<T1, T2, TResult>(token, (Settings)null, parameters);

        public static Delegate ToFunc(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToFunc(Tokenizer.Parse(token, settings), typeParameters, variables, settings, parameters);

        public static Delegate ToFunc(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                        params object[] parameters)
            => ToFunc(token, typeParameters, variables, null, parameters);

        public static Delegate ToFunc(string token, IEnumerable<Type> typeParameters, Settings settings, params object[] parameters)
            => ToFunc(token, typeParameters, null, settings, parameters);

        public static Delegate ToFunc(string token, IEnumerable<Type> typeParameters, params object[] parameters)
            => ToFunc(token, typeParameters, (Settings)null, parameters);

        public static Func<TResult> ToFunc<TResult>(string token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToFunc<TResult>(Tokenizer.Parse(token, settings), variables, settings, parameters);

        public static Func<TResult> ToFunc<TResult>(string token, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc<TResult>(token, variables, null, parameters);

        public static Func<TResult> ToFunc<TResult>(string token, Settings settings, params object[] parameters)
            => ToFunc<TResult>(token, null, settings, parameters);

        public static Func<TResult> ToFunc<TResult>(string token, params object[] parameters)
            => ToFunc<TResult>(token, (Settings)null, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(string token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToFunc<T, TResult>(Tokenizer.Parse(token, settings), variables, settings, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(string token, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc<T, TResult>(token, variables, null, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(string token, Settings settings, params object[] parameters)
            => ToFunc<T, TResult>(token, null, settings, parameters);

        public static Func<T, TResult> ToFunc<T, TResult>(string token, params object[] parameters)
            => ToFunc<T, TResult>(token, (Settings)null, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(string token, IDictionary<string, object> variables, Settings settings, params object[] parameters)
            => ToFunc<T1, T2, TResult>(Tokenizer.Parse(token, settings), variables, settings, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(string token, IDictionary<string, object> variables, params object[] parameters)
            => ToFunc<T1, T2, TResult>(token, variables, null, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(string token, Settings settings, params object[] parameters)
            => ToFunc<T1, T2, TResult>(token, null, settings, parameters);

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(string token, params object[] parameters)
            => ToFunc<T1, T2, TResult>(token, (Settings)null, parameters);

        #endregion
    }
}
