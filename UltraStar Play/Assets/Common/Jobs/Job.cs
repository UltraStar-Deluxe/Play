using System;
using System.Collections.Generic;
using System.Drawing;
using UniRx;

public class Job
{
	public string Name { get; private set; }

    public ReactiveProperty<EJobStatus> Status { get; private set; } = new(EJobStatus.Pending);
    public ReactiveProperty<EJobResult> Result { get; private set; }  = new (EJobResult.Pending);

    private readonly List<Job> childJobs = new();
    public IReadOnlyList<Job> ChildJobs => childJobs;
    public Job ParentJob { get; private set; }

    public Job(string name, Job parentJob = null)
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

        Status.Value = newStatus;
        if (Status.Value == EJobStatus.Finished && Result.Value == EJobResult.Pending)
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

        Result.Value = newResult;
        if (Status.Value != EJobStatus.Finished)
        {
            SetStatus(EJobStatus.Finished);
        }
    }
}
