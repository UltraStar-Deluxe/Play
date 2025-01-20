using System.Collections;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEngine.Awaitable;
using static ConditionTestUtils;

public class JobManagerTest : AbstractPlayModeTest
{
    [Inject]
    private JobManager jobManager;

    private Job parentJob;
    private Job childJob1;
    private Job childJob2;
    private Job childJob3;
    private Job childChildJob1;

    [UnityTest]
    public IEnumerator ShouldFailParentJob() => ShouldFailParentJobAsync();
    private async Awaitable ShouldFailParentJobAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await EnqueueJobs(true);
        await WaitForSecondsAsync(0.5f);

        await StartJob(childJob1);
        await WaitForSecondsAsync(0.5f);

        await FailJob(childJob1);
        await WaitForSecondsAsync(0.5f);

        await ExpectJobResult(EJobResult.Error, childJob1, parentJob);
    }

    [UnityTest]
    public IEnumerator ShouldNotFailParent() => ShouldNotFailParentAsync();
    private async Awaitable ShouldNotFailParentAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await EnqueueJobs(false);
        await WaitForSecondsAsync(0.5f);

        await StartJob(childJob1);
        await WaitForSecondsAsync(0.5f);

        await FailJob(childJob1);
        await WaitForSecondsAsync(0.5f);

        await ExpectJobResult(EJobResult.Error, childJob1);
        await ExpectJobResult(EJobResult.Pending, parentJob);
    }

    private async Awaitable EnqueueJobs(bool adoptChildJobError)
    {
        parentJob = new Job(Translation.Of(nameof(parentJob)));
        parentJob.AdoptChildJobError = adoptChildJobError;

        childJob1 = new Job(Translation.Of(nameof(childJob1)), parentJob);
        childJob2 = new Job(Translation.Of(nameof(childJob2)), parentJob);
        childJob3 = new Job(Translation.Of(nameof(childJob3)), parentJob);
        childChildJob1 = new Job(Translation.Of(nameof(childChildJob1)), childJob3);

        jobManager.AddJob(parentJob);

        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable StartJob(Job job)
    {
        Debug.Log($"Start job {job.Name}");
        childJob1.SetStatus(EJobStatus.Running);
        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable FailJob(Job job)
    {
        Debug.Log($"Fail job {job.Name}");
        job.SetResult(EJobResult.Error);
        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable ExpectJobResult(EJobResult jobResult, params Job[] jobs)
    {
        string jobNameCsv = jobs.Select(job => job.Name).JoinWith(",");
        await WaitForCondition(
            () => jobs.AllMatch(job => job.Result.Value == jobResult),
            new WaitForConditionConfig { description = $"Expect job result {jobResult} for {jobNameCsv}" });
    }
}
