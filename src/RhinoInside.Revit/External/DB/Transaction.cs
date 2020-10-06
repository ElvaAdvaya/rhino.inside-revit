using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;
using Microsoft.PowerShell.Commands;

namespace RhinoInside.Revit.External.DB
{
  public interface ITransactionChainNotification
  {
    void OnChain(Document document);
    void OnPrepare(IReadOnlyCollection<Document> documents);
    void OnDone(TransactionStatus status);
  }

  public struct TransactionHandlingOptions
  {
    //public bool CommitOneByOne;
    public bool ClearAfterRollback;
    public bool DelayedMiniWarnings;
    public bool ForcedModalHandling;
    public IFailuresPreprocessor FailuresPreprocessor;
    public ITransactionFinalizer TransactionFinalizer;
    public ITransactionChainNotification TransactionChainNotification;
  }

  /// <summary>
  /// TransactionChain provide control over a subset of changes on several documents as an atomic unique change.
  /// </summary>
  /// <remarks>
  /// A TransactionChain behaves lia a <see cref="Autodesk.Revit.DB.Transaction"/> but on several documents at
  /// at the same time.
  /// </remarks>
  public class TransactionChain : IFailuresPreprocessor, ITransactionFinalizer, IDisposable
  {
    readonly Dictionary<Document, Transaction> transactionChain = new Dictionary<Document, Transaction>();
    IEnumerator<Transaction> transactionLinks;
    internal readonly string name;

    public bool IsValidObject => transactionChain.All(x => x.Key.IsValidObject && x.Value.IsValidObject);
    public string GetName() => name;
    TransactionHandlingOptions HandlingOptions { get; set; }

    public bool HasStarted() => transactionChain.Count > 0;
    public bool HasStarted(Document doc) =>
      transactionChain.TryGetValue(doc, out var transaction) && transaction.HasStarted();

    public bool HasEnded() => transactionChain.Count == 0;
    public bool HasEnded(Document doc) =>
      transactionChain.TryGetValue(doc, out var transaction) && transaction.HasEnded();

    public TransactionChain() => name = "Unnamed";
    public TransactionChain(Document document) : this()
    {
      Start(document);
    }
    public TransactionChain(params Document[] documents) : this()
    {
      foreach (var doc in documents)
        Start(doc);
    }

    public TransactionChain(string name) => this.name = name;
    public TransactionChain(string name, Document document) :
      this(name)
    {
      Start(document);
    }
    public TransactionChain(string name, params Document[] documents) :
      this(name)
    {
      foreach (var doc in documents)
        Start(doc);
    }

    public TransactionChain(TransactionHandlingOptions options, string name)
    {
      this.name = name;
      this.HandlingOptions = options;
    }
    public TransactionChain(TransactionHandlingOptions options, string name, Document document) :
      this(options, name)
    {
      Start(document);
    }
    public TransactionChain(TransactionHandlingOptions options, string name, params Document[] documents) :
      this(options, name)
    {
      foreach (var doc in documents)
        Start(doc);
    }

    void IDisposable.Dispose()
    {
      // Should not throw any Exception.
      // Commit and Rollback relay on this.

      transactionLinks = default;

      foreach (var transaction in transactionChain.Values.Reverse())
        transaction.Dispose();

      transactionChain.Clear();
    }

    internal TransactionStatus Start(Document doc)
    {
      var result = TransactionStatus.Started;

      if (!transactionChain.ContainsKey(doc))
      {
        var transaction = new Transaction(doc, name);
        try
        {
          result = transaction.Start();
          if (result != TransactionStatus.Started)
          {
            transaction.Dispose();
            throw new InvalidOperationException($"Failed to start Transaction '{name}' on document '{doc.Title.TripleDot(16)}'");
          }

          transaction.SetFailureHandlingOptions
          (
            transaction.GetFailureHandlingOptions().
            SetClearAfterRollback(true).
            SetDelayedMiniWarnings(false).
            SetForcedModalHandling(true).
            SetFailuresPreprocessor(this).
            SetTransactionFinalizer(this)
          );

          HandlingOptions.TransactionChainNotification?.OnChain(doc);

          transactionChain.Add(doc, transaction);
        }
        catch (Exception e)
        {
          transaction.Dispose();
          throw e;
        }
      }

      return result;
    }

    public TransactionStatus Commit()
    {
      if (transactionChain.Count == 0)
        return TransactionStatus.Uninitialized;

      var status = TransactionStatus.Error;
      try
      {
        using (this)
        {
          HandlingOptions.TransactionChainNotification?.OnPrepare(transactionChain.Keys);

          using (transactionLinks = transactionChain.Values.GetEnumerator())
            status = CommitNextTransaction();
        }
      }
      finally
      {
        HandlingOptions.TransactionChainNotification?.OnDone(status);
      }

      return status;
    }

    public TransactionStatus RollBack()
    {
      if (transactionChain.Count == 0)
        return TransactionStatus.Uninitialized;

      var status = TransactionStatus.Error;
      try
      {
        using (this)
        {
          foreach (var transaction in transactionChain.Values.Reverse())
            transaction.RollBack();

          status = TransactionStatus.RolledBack;
        }
      }
      finally
      {
        HandlingOptions.TransactionChainNotification?.OnDone(status);
      }

      return status;
    }

    #region ITransactionChainNotification
    TransactionStatus CommitNextTransaction()
    {
      if (transactionLinks.MoveNext())
      {
        var transaction = transactionLinks.Current;

        if (transaction.GetStatus() == TransactionStatus.Started)
          return transaction.Commit();
        else
          return transaction.RollBack();
      }

      return TransactionStatus.Committed;
    }

    FailureProcessingResult IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
    {
      var result = HandlingOptions.FailuresPreprocessor?.PreprocessFailures(failuresAccessor) ??
                   FailureProcessingResult.Continue;

      if (transactionChain.ContainsKey(failuresAccessor.GetDocument()) == true)
      {
        if (result < FailureProcessingResult.ProceedWithRollBack)
        {
          result = failuresAccessor.IsTransactionBeingCommitted() &&
                   CommitNextTransaction() == TransactionStatus.Committed ?
            FailureProcessingResult.Continue :
            FailureProcessingResult.ProceedWithRollBack;
        }
      }

      return result;
    }
    #endregion

    #region ITransactionFinalizer
    void ITransactionFinalizer.OnCommitted(Document document, string strTransactionName)
    {
      HandlingOptions.TransactionFinalizer?.OnCommitted(document, strTransactionName);
    }

    void ITransactionFinalizer.OnRolledBack(Document document, string strTransactionName)
    {
      HandlingOptions.TransactionFinalizer?.OnRolledBack(document, strTransactionName);
    }
    #endregion
  }

  /// <summary>
  /// Adaptive-transactions are objects that provide control over a subset of changes in a document.
  /// </summary>
  /// <remarks>
  /// An AdaptiveTransaction behaves like a <see cref="Autodesk.Revit.DB.Transaction"/> in case the
  /// <see cref="Autodesk.Revit.DB.Document"/> has no active Transaction running on it, otherwise
  /// as a <see cref="Autodesk.Revit.DB.SubTransaction"/>.
  /// </remarks>
  public class AdaptiveTransaction : IDisposable
  {
    readonly Document document;
    Transaction transaction;
    SubTransaction subTransaction;

    public AdaptiveTransaction(Document doc) => document = doc;

    public bool IsValidObject => transaction?.IsValidObject != false && subTransaction?.IsValidObject != false;
    public TransactionStatus GetStatus()
    {
      if (transaction is object) return transaction.GetStatus();
      if (subTransaction is object) return subTransaction.GetStatus();
      return TransactionStatus.Uninitialized;
    }

    public bool HasStarted()
    {
      if (transaction is object) return transaction.HasStarted();
      if (subTransaction is object) return subTransaction.HasStarted();
      return false;
    }

    public bool HasEnded()
    {
      if (transaction is object) return transaction.HasEnded();
      if (subTransaction is object) return subTransaction.HasEnded();
      return true;
    }

    public TransactionStatus Start()
    {
      if (HasStarted())
        throw new InvalidOperationException("AdaptiveTransaction is already started.");

      TransactionStatus status;

      if (document.IsModifiable)
      {
        var subtr = new SubTransaction(document);
        status = subtr.Start();
        subTransaction = subtr;
      }
      else
      {
        var trans = new Transaction(document, "Adaptive Transaction");
        status = trans.Start();
        transaction = trans;
      }

      return status;
    }

    public TransactionStatus Commit()
    {
      if (!HasStarted())
        throw new InvalidOperationException("AdaptiveTransaction is not started.");

      using (this)
      {
        if (transaction is object) return transaction.Commit();
        if (subTransaction is object) return subTransaction.Commit();
        return TransactionStatus.Uninitialized;
      }
    }

    public TransactionStatus RollBack()
    {
      if (!HasStarted())
        throw new InvalidOperationException("AdaptiveTransaction is not started.");

      using (this)
      {
        if (transaction is object) return transaction.RollBack();
        if (subTransaction is object) return subTransaction.RollBack();
        return TransactionStatus.Uninitialized;
      }
    }

    public void Dispose()
    {
      transaction?.Dispose();
      transaction = default;

      subTransaction?.Dispose();
      subTransaction = default;
    }
  }

  public static class DisposableScope
  {
    /// <summary>
    /// Replacement for default(IDisposable).
    /// </summary>
    /// <remarks>
    /// IronPython 'with' keyword implementation doesn't check for null before calling Dispose like C# compiler does.
    /// </remarks>
    private class Disposable : IDisposable
    {
      internal static readonly IDisposable Default = new Disposable();
      Disposable() { }
      void IDisposable.Dispose() {}
    }

    /// <summary>
    /// Implementation class for <see cref="CommitScope(Document)"/>
    /// </summary>
    private class AutoScope : IDisposable
    {
      readonly Transaction transaction;

      internal AutoScope(Document document)
      {
        var name = "Commit Scope";
        transaction = new Transaction(document, name);
        transaction.SetFailureHandlingOptions
        (
          transaction.GetFailureHandlingOptions().
          SetClearAfterRollback(true).
          SetClearAfterRollback(false).
          SetForcedModalHandling(true)
        );
        if (transaction.Start() != TransactionStatus.Started)
          throw new InvalidOperationException($"Failed to start Transaction '{name}' on document '{document.Title.TripleDot(16)}'");
      }

      void IDisposable.Dispose()
      {
        using (transaction)
        {
          if (Marshal.GetExceptionCode() == 0)
            transaction.Commit();
          else
            transaction.RollBack();
        }
      }
    }

    /// <summary>
    /// Starts a Commit scope that will be automatically comitted when disposed. In case of exception it will be rolledback.
    /// </summary>
    /// <param name="document"></param>
    /// <returns><see cref="IDisposable"/> that should be disposed to maked efective all changes done to <paramref name="document"/> in the scope.</returns>
    /// <remarks>
    /// Use an auto dispose pattern to be sure the returned <see cref="IDisposable"/> is disposed before the calling method returns.
    /// <para>
    /// C# : using(document.CommitScope())
    /// </para>
    /// <para>
    /// VB : Using document.CommitScope()
    /// </para>
    /// <para>
    /// Pyhton: with document.CommitScope() :
    /// </para>
    /// </remarks>
    public static IDisposable CommitScope(this Document document)
    {
      return document.IsModifiable ? Disposable.Default : new AutoScope(document);
    }

    /// <summary>
    /// Starts a RollBack scope that will be automatically rolled back when disposed.
    /// </summary>
    /// <param name="document"></param>
    /// <returns><see cref="IDisposable"/> that should be disposed to rollback all changes done to <paramref name="document"/> in the scope.</returns>
    /// <remarks>
    /// Use an auto dispose pattern to be sure the returned <see cref="IDisposable"/> is disposed before the calling method returns.
    /// <para>
    /// C# : using(document.CommitScope())
    /// </para>
    /// <para>
    /// VB : Using document.CommitScope()
    /// </para>
    /// <para>
    /// Pyhton: with document.CommitScope() :
    /// </para>
    /// </remarks>
    public static IDisposable RollBackScope(this Document document)
    {
      var name = "RollBack Scope";
      if (document.IsModifiable)
      {
        var subTransaction = new SubTransaction(document);
        if (subTransaction.Start() != TransactionStatus.Started)
          throw new InvalidOperationException($"Failed to start subTransaction '{name}' on document '{document.Title.TripleDot(16)}'");

        return subTransaction;
      }
      else
      {
        var transaction = new Transaction(document, name);
        transaction.SetFailureHandlingOptions
        (
          transaction.GetFailureHandlingOptions().
          SetClearAfterRollback(true).
          SetClearAfterRollback(false).
          SetForcedModalHandling(true)
        );
        if (transaction.Start() != TransactionStatus.Started)
          throw new InvalidOperationException($"Failed to start Transaction '{name}' on document '{document.Title.TripleDot(16)}'");

        return transaction;
      }
    }
  }
}
