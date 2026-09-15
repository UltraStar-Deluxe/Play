using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEngine.Awaitable;
using static ConditionUtils;

public class JobSequenceTest : AbstractPlayModeTest
{
    private const int DummyTaskDurationInSeconds = 2;

    [Inject]
    private JobManager jobManager;

    private Job<VoidEvent> parentJob;

    private Job<JobTiming> childJob1;
    private JobTiming childJob1JobTiming;

    private Job<JobTiming> childJob2;
    private JobTiming childJob2JobTiming;

    [UnityTest]
    public IEnumerator ChildJobsShouldExecuteInSequence() => ChildJobsShouldExecuteInSequenceAsync();
    private async Awaitable ChildJobsShouldExecuteInSequenceAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given: Job with child jobs
        EnqueueJobs();
        await WaitForSecondsAsync(1);

        // When: Job started
        await RunJobsAsync();

        // Then
        double precisionFactor = 0.5; // Actual duration can vary slightly
        long expectedTaskDurationInMillis = (long)(DummyTaskDurationInSeconds * 1000.0 * precisionFactor);
        Assert.IsTrue(childJob1JobTiming.endTime >= childJob1JobTiming.startTime + expectedTaskDurationInMillis,
            $"childJob1 should have taken some time, {childJob1JobTiming}");
        Assert.IsTrue(childJob2JobTiming.endTime >= childJob2JobTiming.startTime + expectedTaskDurationInMillis,
            $"childJob2 should have taken some time, {childJob2JobTiming}");
        Assert.IsTrue(childJob2JobTiming.startTime >= childJob1JobTiming.endTime,
            $"childJob2 should have started after childJob1 has finished, childJob1: {childJob1JobTiming}, childJob2: {childJob2JobTiming}");
    }

    private async Awaitable RunJobsAsync()
    {
        await parentJob.RunAsync();
    }

    private void EnqueueJobs()
    {
        parentJob = new Job<VoidEvent>(Translation.Of(nameof(parentJob)));

        childJob1JobTiming = new();
        childJob1 = CreateChildJob(nameof(childJob1), parentJob, childJob1JobTiming);
        childJob2JobTiming = new();
        childJob2 = CreateChildJob(nameof(childJob2), parentJob, childJob2JobTiming);

        jobManager.AddJob(parentJob);
    }

    private static Job<JobTiming> CreateChildJob(string name, IJob parentJob, JobTiming jobTiming)
    {
        Job<JobTiming> childJob = new Job<JobTiming>(
            Translation.Of(name),
            () => DummyTaskAsync(name, jobTiming));
        parentJob.AddChildJob(childJob);
        return childJob;
    }

    private static async Awaitable<JobTiming> DummyTaskAsync(string name, JobTiming jobTiming)
    {
        jobTiming.startTime = TimeUtils.GetUnixTimeMilliseconds();
        await WaitForSecondsAsync(DummyTaskDurationInSeconds);
        jobTiming.endTime = TimeUtils.GetUnixTimeMilliseconds();
        Debug.Log($"Dummy task done: job '{name}', startTime: {jobTiming.startTime}, endTime: {jobTiming.endTime}");
        return jobTiming;
    }

    private class JobTiming
    {
        public long startTime;
        public long endTime;

        public override string ToString()
        {
            return $"startTime: {startTime}, endTime: {endTime}";
        }
    }
}
