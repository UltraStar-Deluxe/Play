using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;

public class Job<T> : IJob
{
    /**
     * Getter for the Awaitable that should be executed.
     * A Getter is used instead of the Awaitable directly to avoid premature execution.
     */
    private Func<Awaitable<T>> awaitableProvider;

    public Translation Name { get; }
    public JobProgress Progress { get; }

    public bool AdoptChildJobError { get; set; } = true;

    private CancellationTokenSource CancellationTokenSource => Progress?.CancellationTokenSource;
    private bool IsCancellationRequested => CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested;

    public ReactiveProperty<EJobStatus> Status { get; private set; } = new(EJobStatus.Pending);
    public ReactiveProperty<EJobResult> Result { get; private set; }  = new (EJobResult.Pending);

    private readonly List<IJob> childJobs = new();
    public IReadOnlyList<IJob> ChildJobs => childJobs;

    private IJob parentJob;

    public IJob ParentJob
    {
        get => parentJob;
        set
        {
            if (Status.Value is not EJobStatus.Pending)
            {
                throw new InvalidOperationException("Job has already been started. Cannot change parent job.");
            }

            if (parentJob == value)
            {
                return;
            }
            if (parentJob != null)
            {
                throw new InvalidOperationException("Cannot change parent job");
            }
            parentJob = value;
            parentJob.AddChildJob(this);
        }
    }

    public ReactiveProperty<bool> IsCanceled { get; }
    public ReactiveProperty<bool> IsCancelable { get; }

    /**
     * Constructor intended to set the Awaitable later.
     */
    public Job(
        Translation name,
        CancellationTokenSource cancellationTokenSource)
        : this(name, null, cancellationTokenSource)
    {
    }

    public Job(
        Translation name,
        Func<Awaitable<T>> awaitableProvider = null,
        CancellationTokenSource cancellationTokenSource = null)
    {
        Name = name;
        this.awaitableProvider = awaitableProvider;
        this.Progress = new JobProgress(cancellationTokenSource);
        IsCanceled = new ReactiveProperty<bool>(false);
        IsCancelable = new ReactiveProperty<bool>(CancellationTokenSource != null);
    }

    public async Awaitable RunAsync()
    {
        await GetResultAsync();
    }

    public async Awaitable<T> GetResultAsync()
    {
        if (Status.Value != EJobStatus.Pending)
        {
            throw new InvalidOperationException($"Can only start a job that is in a pending state: job '{Name}', status {Status.Value}");
        }

        try
        {
            T result = default;
            SetStatus(EJobStatus.Running);

            Awaitable<T> awaitable = awaitableProvider?.Invoke();

            if (awaitable == null
                && childJobs.IsNullOrEmpty())
            {
                throw new InvalidOperationException($"Job is missing awaitable to be executed: job '{Name}'");
            }

            if (awaitable != null)
            {
                result = await awaitable;
            }
            if (!childJobs.IsNullOrEmpty())
            {
                await RunChildJobsAsync();
            }

            CancellationTokenSource?.Token.ThrowIfCancellationRequested();
            SetResult(EJobResult.Ok);

            return result;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Job '{Name}' failed: {ex.Message}");
            SetResult(EJobResult.Error);
            throw ex;
        }
        finally
        {
            if (Status.Value != EJobStatus.Finished)
            {
                SetStatus(EJobStatus.Finished);
            }
        }
    }

    public void SetAwaitable(Func<Awaitable<T>> newValue)
    {
        if (Status.Value is not EJobStatus.Pending)
        {
            throw new InvalidOperationException("Job has already been started. Cannot change awaitable.");
        }

        awaitableProvider = newValue;
    }

    public void AddChildJob(IJob childJob)
    {
        if (Status.Value is not EJobStatus.Pending)
        {
            throw new InvalidOperationException("Job has already been started. Cannot add child jobs.");
        }

        if (this == childJob)
        {
            throw new ArgumentException("Cannot add self as child job.");
        }

        if (childJobs.Contains(childJob))
        {
            // Avoid recursion with setting parentJob
            return;
        }

        childJobs.Add(childJob);
        childJob.ParentJob = this;

        childJob.Result.Subscribe(_ => OnChildJobChanged());
        childJob.Status.Subscribe(_ => OnChildJobChanged());
    }

    private async Awaitable RunChildJobsAsync()
    {
        if (AdoptChildJobError)
        {
            foreach (IJob childJob in childJobs)
            {
                if (childJob.Result.Value is EJobResult.Pending)
                {
                    await childJob.RunAsync();
                }
                CancellationTokenSource?.Token.ThrowIfCancellationRequested();
            }
        }
        else
        {
            foreach (IJob childJob in childJobs)
            {
                try
                {
                    if (childJob.Result.Value is EJobResult.Pending)
                    {
                        await childJob.RunAsync();
                    }
                }
                catch (Exception ex)
                {
                    ex.Log($"Child job failed, continuing with remaining child jobs: parent job '{Name}', failed child job '{childJob.Name}'");
                }
                CancellationTokenSource?.Token.ThrowIfCancellationRequested();
            }
        }
    }

    private void OnChildJobChanged()
    {
        bool anyChildHasError = false;
        bool anyChildRunning = false;
        bool allChildrenFinished = true;
        foreach (IJob childJob in childJobs)
        {
            if (childJob.Result.Value == EJobResult.Error)
            {
                anyChildHasError = true;
            }
            else if (childJob.Result.Value != EJobResult.Ok)
            {
                allChildrenFinished = false;
            }

            if (childJob.Status.Value == EJobStatus.Running)
            {
                anyChildRunning = true;
            }
        }

        if (anyChildHasError
            && AdoptChildJobError)
        {
            SetResult(EJobResult.Error);
        }
        else if (allChildrenFinished)
        {
            SetResult(EJobResult.Ok);
        }
        else if (anyChildRunning
                 && Status.Value is EJobStatus.Pending)
        {
            SetStatus(EJobStatus.Running);
        }
    }

    private void SetStatus(EJobStatus newStatus)
    {
        if (Status.Value == newStatus)
        {
            return;
        }

        if (Status.Value is EJobStatus.Pending
            && newStatus != EJobStatus.Running
            && newStatus != EJobStatus.Finished
            || Status.Value is EJobStatus.Running
            && newStatus != EJobStatus.Finished)
        {
            throw new InvalidOperationException($"Cannot change state from {Status.Value} to {newStatus}");
        }

        if (newStatus == EJobStatus.Running)
        {
            Progress.StartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        }

        Status.Value = newStatus;
        if (Status.Value == EJobStatus.Finished
            && Result.Value == EJobResult.Pending)
        {
            SetResult(EJobResult.Ok);
        }
    }

    private void SetResult(EJobResult newResult)
    {
        if (Result.Value == newResult)
        {
            return;
        }

        if ((Result.Value is EJobResult.Pending
                && newResult != EJobResult.Ok
                && newResult != EJobResult.Error)
            || Result.Value is EJobResult.Ok or EJobResult.Error)
        {
            throw new InvalidOperationException($"Cannot change result from {Result.Value} to {newResult}");
        }

        // Cancel job if the result is set to error
        if (newResult is EJobResult.Error
            && Status.Value is not EJobStatus.Finished)
        {
            try
            {
                Cancel();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Failed to cancel job after setting result to error");
            }
        }

        Result.Value = newResult;
        Progress.EndTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        if (Status.Value != EJobStatus.Finished)
        {
            SetStatus(EJobStatus.Finished);
        }
    }

    public void Cancel()
    {
        if (IsCanceled.Value
            || IsCancellationRequested)
        {
            Debug.LogWarning($"Job is already canceled: name '{Name}'");
            return;
        }

        if (!IsCancelable.Value)
        {
            Debug.LogWarning($"Job cannot be canceled: name '{Name}'");
            return;
        }

        Debug.Log($"Cancelling job '{Name}'");
        IsCanceled.Value = true;
        CancellationTokenSource?.Cancel();

        CancelChildJobs();
    }

    private void CancelChildJobs()
    {
        foreach (IJob childJob in childJobs)
        {
            childJob?.Cancel();
        }
    }
}
