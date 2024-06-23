using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class Job
{
	public Translation Name { get; private set; }

    public ReactiveProperty<EJobStatus> Status { get; private set; } = new(EJobStatus.Pending);
    public ReactiveProperty<EJobResult> Result { get; private set; }  = new (EJobResult.Pending);

    private readonly List<Job> childJobs = new();
    public IReadOnlyList<Job> ChildJobs => childJobs;
    public Job ParentJob { get; private set; }

    public long EstimatedTotalDurationInMillis { get; set; }
    public double EstimatedCurrentProgressInPercent
    {
        get
        {
            if (endTimeInMillis > 0)
            {
                return 100;
            }

            if (EstimatedTotalDurationInMillis <= 0
                || startTimeInMillis == 0)
            {
                return 0;
            }

            double progressInPercent = 100.0 * (double)CurrentDurationInMillis / EstimatedTotalDurationInMillis;
            if (progressInPercent > 99)
            {
                progressInPercent = 99;
            }
            return progressInPercent;
        }

        set
        {
            double progressFactor = value / 100.0;
            if (progressFactor <= 0)
            {
                return;
            }
            EstimatedTotalDurationInMillis = (long)(CurrentDurationInMillis * (1 / progressFactor));
        }
    }

    public long CurrentDurationInMillis
    {
        get
        {
            if (startTimeInMillis == 0)
            {
                return 0;
            }
            if (endTimeInMillis > 0)
            {
                return endTimeInMillis - startTimeInMillis;
            }
            return TimeUtils.GetUnixTimeMilliseconds() - startTimeInMillis;
        }
    }

    private long startTimeInMillis;
    private long endTimeInMillis;

    private Action onCancel;
    public Action OnCancel {
        get
        {
            return onCancel;
        }
        set
        {
            onCancel = value;
            IsCancelable.Value = onCancel != null;
        }
    }
    public ReactiveProperty<bool> IsCanceled { get; private set; } = new(false);
    public ReactiveProperty<bool> IsCancelable { get; private set; } = new(false);

    public Job(Translation name, Job parentJob = null)
    {
        Name = name;
        if (parentJob != null)
        {
            parentJob.AddChildJob(this);
        }
    }

    private void AddChildJob(Job childJob)
    {
        childJob.ParentJob = this;
        childJobs.Add(childJob);

        childJob.Result.Subscribe(_ => OnChildJobChanged());
        childJob.Status.Subscribe(_ => OnChildJobChanged());
    }

    private void OnChildJobChanged()
    {
        bool anyChildHasError = false;
        bool anyChildRunning = false;
        bool allChildrenFinished = true;
        foreach (Job childJob in childJobs)
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

        if (anyChildHasError)
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

    public void SetStatus(EJobStatus newStatus)
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
            startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        }

        Status.Value = newStatus;
        if (Status.Value == EJobStatus.Finished
            && Result.Value == EJobResult.Pending)
        {
            SetResult(EJobResult.Ok);
        }
    }

    public void SetResult(EJobResult newResult)
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
        endTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        if (Status.Value != EJobStatus.Finished)
        {
            SetStatus(EJobStatus.Finished);
        }
    }

    public void SetResultIfPending(EJobResult newResult)
    {
        if (Result.Value == EJobResult.Pending)
        {
            SetResult(newResult);
        }
    }

    public void Cancel()
    {
        if (IsCanceled.Value
            || !IsCancelable.Value)
        {
            return;
        }

        IsCanceled.Value = true;
        try
        {
            Debug.Log($"Cancelling job '{Name}'");
            onCancel?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to cancel job '{Name}': {ex.Message}");
        }
    }
}
