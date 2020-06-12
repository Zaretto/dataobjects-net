// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.11.26

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Xtensive.Core;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Providers;
using Xtensive.Reflection;

namespace Xtensive.Orm.Linq
{
  /// <summary>
  /// <see cref="IQueryProvider"/> implementation.
  /// </summary>
  public sealed class QueryProvider : IQueryProvider
  {
    /// <summary>
    /// Gets <see cref="Session"/> this provider is attached to.
    /// </summary>
    public Session Session { get; }

    /// <inheritdoc/>
    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
      var elementType = SequenceHelper.GetElementType(expression.Type);
      try {
        var query = (IQueryable) WellKnown.Types.QueryableOfT.Activate(new[] {elementType}, this, expression);
        return query;
      }
      catch (TargetInvocationException e) {
        if (e.InnerException != null) {
          ExceptionDispatchInfo.Throw(e.InnerException);
        }

        throw;
      }
    }

    /// <inheritdoc/>
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
      new Queryable<TElement>(this, expression);

    /// <inheritdoc/>
    object IQueryProvider.Execute(Expression expression)
    {
      var iEnumerableOfT = WellKnown.Interfaces.EnumerableOfT;
      var resultType = expression.Type.IsOfGenericInterface(iEnumerableOfT)
        ? iEnumerableOfT.MakeGenericType(expression.Type.GetGenericArguments())
        : expression.Type;
      try {
        var executeMethod = WellKnownMembers.QueryProvider.Execute.MakeGenericMethod(resultType);
        var result = executeMethod.Invoke(this, new object[] {expression});
        return result;
      }
      catch (TargetInvocationException e) {
        if (e.InnerException != null) {
          ExceptionDispatchInfo.Throw(e.InnerException);
        }

        throw;
      }
    }

    /// <inheritdoc/>
    public TResult Execute<TResult>(Expression expression)
    {
      static TResult ExecuteScalarQuery(
        TranslatedQuery query, Session session, ParameterContext parameterContext)
      {
        return query.ExecuteScalar<TResult>(session, parameterContext);
      }

      return Execute(expression, ExecuteScalarQuery);
    }

    public SequenceQueryResult<T> ExecuteSequence<T>(Expression expression)
    {
      static SequenceQueryResult<T> ExecuteSequenceQuery(
        TranslatedQuery query, Session session, ParameterContext parameterContext)
      {
        return query.ExecuteSequence<T>(session, parameterContext);
      }

      return Execute(expression, ExecuteSequenceQuery);
    }

    private TResult Execute<TResult>(Expression expression,
      Func<TranslatedQuery, Session, ParameterContext, TResult> runQuery)
    {
      expression = Session.Events.NotifyQueryExecuting(expression);
      var query = Translate(expression);
      TResult result;
      var compiledQueryScope = CompiledQueryProcessingScope.Current;
      if (compiledQueryScope != null && !compiledQueryScope.Execute) {
        result = default;
      }
      else {
        try {
          result = runQuery(query, Session, compiledQueryScope?.ParameterContext ?? new ParameterContext());
        }
        catch (Exception exception) {
          Session.Events.NotifyQueryExecuted(expression, exception);
          throw;
        }
      }
      Session.Events.NotifyQueryExecuted(expression);
      return result;
    }

    public Task<TResult> ExecuteScalarAsync<TResult>(Expression expression, CancellationToken token)
    {
      static Task<TResult> ExecuteScalarQueryAsync(
        TranslatedQuery query, Session session, ParameterContext parameterContext, CancellationToken token)
      {
        return query.ExecuteScalarAsync<TResult>(session, parameterContext, token);
      }
      return ExecuteAsync(expression, ExecuteScalarQueryAsync, token);
    }

    public Task<SequenceQueryResult<T>> ExecuteSequenceAsync<T>(Expression expression, CancellationToken token)
    {
      static Task<SequenceQueryResult<T>> ExecuteSequenceQueryAsync(
        TranslatedQuery query, Session session, ParameterContext parameterContext, CancellationToken token)
      {
        return query.ExecuteSequenceAsync<T>(session, parameterContext, token);
      }
      return ExecuteAsync(expression, ExecuteSequenceQueryAsync, token);
    }

    private async Task<TResult> ExecuteAsync<TResult>(Expression expression,
      Func<TranslatedQuery, Session, ParameterContext, CancellationToken, Task<TResult>> runQuery, CancellationToken token)
    {
      expression = Session.Events.NotifyQueryExecuting(expression);
      var query = Translate(expression);
      TResult result;
      try {
        result = await runQuery(query, Session, new ParameterContext(), token).ConfigureAwait(false);
      }
      catch (Exception exception) {
        Session.Events.NotifyQueryExecuted(expression, exception);
        throw;
      }

      Session.Events.NotifyQueryExecuted(expression);
      return result;
    }

    #region Private / internal methods

    internal TranslatedQuery Translate(Expression expression) =>
      Translate(expression, Session.CompilationService.CreateConfiguration(Session));

    internal TranslatedQuery Translate(Expression expression,
      CompilerConfiguration compilerConfiguration)
    {
      try {
        var compiledQueryScope = CompiledQueryProcessingScope.Current;
        var context = new TranslatorContext(Session, compilerConfiguration, expression, compiledQueryScope);
        return context.Translator.Translate();
      }
      catch (Exception ex) {
        throw new QueryTranslationException(string.Format(
          Strings.ExUnableToTranslateXExpressionSeeInnerExceptionForDetails, expression.ToString(true)), ex);
      }
    }

    #endregion


    // Constructors

    internal QueryProvider(Session session)
    {
      Session = session;
    }
  }
}