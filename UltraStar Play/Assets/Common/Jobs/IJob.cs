using System.Collections.Generic;
using UniRx;
using UnityEngine;

public interface IJob
{
    Translation Name { get; }

    public IJob ParentJob { get; set; }
    public IReadOnlyList<IJob> ChildJobs { get; }
    public void AddChildJob(IJob job);

    public ReactiveProperty<EJobResult> Result { get; }
    public ReactiveProperty<EJobStatus> Status { get; }

    public ReactiveProperty<bool> IsCancelable { get; }
    public ReactiveProperty<bool> IsCanceled { get; }
    JobProgress Progress { get; }
    public void Cancel();
    Awaitable RunAsync();
}
