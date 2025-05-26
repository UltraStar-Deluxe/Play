using System;
using System.Collections;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEngine.Awaitable;
using static ConditionUtils;

public class JobSequenceContinuationTest : AbstractPlayModeTest
{
    [Inject]
    private JobManager jobManager;

    private Job<VoidEvent> parentJob;
    private Job<string> childJob1ThrowsException;
    private Job<string> childJob2;
    private Job<string> childJob3;
    private Job<string> childJob3ChildJob;

    [UnityTest]
    public IEnumerator ShouldFailParentJob() => ShouldFailParentJobAsync();
    private async Awaitable ShouldFailParentJobAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given: Job with child job
        EnqueueJobs(true);
        await WaitForSecondsAsync(1);

        // When: Job started
        await RunJobsAsync();

        // Then
        await ExpectJobResultAsync(EJobResult.Error, childJob1ThrowsException, parentJob);
    }

    [UnityTest]
    public IEnumerator ShouldNotFailParent() => ShouldNotFailParentAsync();
    private async Awaitable ShouldNotFailParentAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given: Job with child job
        EnqueueJobs(false);
        await WaitForSecondsAsync(1);

        // When: Job started
        await RunJobsAsync();

        // Then
        await ExpectJobResultAsync(EJobResult.Error, childJob1ThrowsException);
        await ExpectJobResultAsync(EJobResult.Ok, parentJob, childJob2, childJob3, childJob3ChildJob);
    }

    private async Awaitable RunJobsAsync()
    {
        try
        {
            await parentJob.RunAsync();
        }
        catch (Exception ex)
        {
            // Exception is expected here because childJob1 throws an exception
            ex.Log();
        }
    }

    private void EnqueueJobs(bool adoptChildJobError)
    {
        parentJob = new Job<VoidEvent>(Translation.Of(nameof(parentJob)));
        parentJob.AdoptChildJobError = adoptChildJobError;

        childJob1ThrowsException = CreateChildJob(nameof(childJob1ThrowsException), parentJob, "dummy exception");
        childJob2 = CreateChildJob(nameof(childJob2), parentJob);
        childJob3 = CreateChildJob(nameof(childJob3), parentJob);
        childJob3ChildJob = CreateChildJob(nameof(childJob3ChildJob), childJob3);

        jobManager.AddJob(parentJob);
    }

    private async Awaitable ExpectJobResultAsync(EJobResult jobResult, params IJob[] jobs)
    {
        string jobNameCsv = jobs.Select(job => job.Name).JoinWith(",");
        await WaitForConditionAsync(
            () => jobs.AllMatch(job => job.Result.Value == jobResult),
            new WaitForConditionConfig { description = $"Expect job result {jobResult} for {jobNameCsv}" });
    }

    private static Job<string> CreateChildJob(string name, IJob parentJob, string exceptionMessage = null)
    {
        Job<string> childJob = new Job<string>(
            Translation.Of(name),
            () => DummyTaskAsync(name, exceptionMessage));
        parentJob.AddChildJob(childJob);
        return childJob;
    }

    private static async Awaitable<string> DummyTaskAsync(string name, string exceptionMessage = null)
    {
        await WaitForSecondsAsync(1);
        if (!exceptionMessage.IsNullOrEmpty())
        {
            throw new Exception(exceptionMessage);
        }

        return $"done doing something for {name}";
    }
}
