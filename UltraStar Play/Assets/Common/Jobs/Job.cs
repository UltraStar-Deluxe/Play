using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;

public class Job<T> : IJob
{
    private readonly Awaitable<T> awaitable;
    public Translation Name { get; }
    public JobProgress Progress { get; set; }
    public CancellationTokenSource CancellationTokenSource => Progress?.CancellationTokenSource;
    public bool IsCancellationRequested => CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested;

    public ReactiveProperty<EJobStatus> Status { get; private set; } = new(EJobStatus.Pending);
    public ReactiveProperty<EJobResult> Result { get; private set; }  = new (EJobResult.Pending);

    private readonly List<IJob> childJobs = new();
    public IReadOnlyList<IJob> ChildJobs => childJobs;
    public IJob ParentJob { get; set; }

    public bool AdoptChildJobError { get; set; }

    public ReactiveProperty<bool> IsCanceled { get; }
    public ReactiveProperty<bool> IsCancelable { get; }

    public Job(
        Translation name,
        Awaitable<T> awaitable = null,
        JobProgress jobProgress = null,
        IJob parentJob = null)
    {
        Name = name;
        this.awaitable = awaitable;
        this.Progress = jobProgress ?? new JobProgress(null);
        IsCanceled = new ReactiveProperty<bool>(false);
        IsCancelable = new ReactiveProperty<bool>(IsCancellationRequested);
        if (parentJob != null)
        {
            parentJob.AddChildJob(this);
        }
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

            if (awaitable != null)
            {
                result = await awaitable;
            }
            await RunChildJobsAsync();

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

    public void AddChildJob(IJob childJob)
    {
        childJob.ParentJob = this;
        childJobs.Add(childJob);

        childJob.Result.Subscribe(_ => OnChildJobChanged());
        childJob.Status.Subscribe(_ => OnChildJobChanged());
    }

    private async Awaitable RunChildJobsAsync()
    {
        if (AdoptChildJobError)
        {
            foreach (IJob childJob in childJobs)
            {
                await childJob.RunAsync();
            }
        }
        else
        {
            foreach (IJob childJob in childJobs)
            {
                try
                {
                    await childJob.RunAsync();
                }
                catch (Exception ex)
                {
                    ex.Log($"Child job failed, continuing with remaining child jobs: parent job '{Name}', failed child job '{childJob.Name}'");
                }
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
            throw new IllegalStateException($"Cannot change state from {Status.Value} to {newStatus}");
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
            throw new IllegalStateException($"Cannot change result from {Result.Value} to {newResult}");
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
            || !IsCancelable.Value
            || IsCancellationRequested)
        {
            return;
        }

        Debug.Log($"Cancelling job '{Name}'");
        IsCanceled.Value = true;
        CancellationTokenSource?.Cancel();
    }
}
