using System.Collections;
using System.Linq;
using Responsible;
using UniInject;
using UnityEngine.TestTools;
using static Responsible.Responsibly;
using static ResponsibleLogAssertUtils;

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
    public IEnumerator ShouldFailParentJob() => IgnoreFailingMessages()
        .ContinueWith(_ => EnqueueJobs(true))
        .ContinueWith(_ => WaitForSeconds(0.5))
        .ContinueWith(_ => StartJob(childJob1))
        .ContinueWith(_ => WaitForSeconds(0.5))
        .ContinueWith(_ => FailJob(childJob1))
        .ContinueWith(_ => WaitForSeconds(0.5))
        .ContinueWith(_ => ExpectJobResult(EJobResult.Error, childJob1, parentJob))
        .ToYieldInstruction(Executor);

    [UnityTest]
    public IEnumerator ShouldNotFailParent() => IgnoreFailingMessages()
        .ContinueWith(_ => EnqueueJobs(false))
        .ContinueWith(_ => WaitForSeconds(0.5))
        .ContinueWith(_ => StartJob(childJob1))
        .ContinueWith(_ => WaitForSeconds(0.5))
        .ContinueWith(_ => FailJob(childJob1))
        .ContinueWith(_ => WaitForSeconds(0.5))
        .ContinueWith(_ => ExpectJobResult(EJobResult.Error, childJob1))
        .ContinueWith(_ => ExpectJobResult(EJobResult.Pending, parentJob))
        .ToYieldInstruction(Executor);

    private ITestInstruction<object> EnqueueJobs(bool adoptChildJobError) =>
        Do($"create jobs",
            () =>
            {
                parentJob = new Job(Translation.Of(nameof(parentJob)));
                parentJob.AdoptChildJobError = adoptChildJobError;
                childJob1 = new Job(Translation.Of(nameof(childJob1)), parentJob);
                childJob2 = new Job(Translation.Of(nameof(childJob2)), parentJob);
                childJob3 = new Job(Translation.Of(nameof(childJob3)), parentJob);
                childChildJob1 = new Job(Translation.Of(nameof(childChildJob1)), childJob3);

                jobManager.AddJob(parentJob);
            });

    private ITestInstruction<object> StartJob(Job job) =>
        Do($"Start job {job.Name}",
            () =>
            {
                childJob1.SetStatus(EJobStatus.Running);
            });

    private ITestInstruction<object> FailJob(Job job) =>
        Do($"Fail job {job.Name}",
            () => job.SetResult(EJobResult.Error));

    private ITestInstruction<object> FinishJob(Job job) =>
        Do($"Finish job {job.Name}",
            () =>
            {
                childJob1.SetStatus(EJobStatus.Finished);
            });

    private ITestInstruction<object> ExpectJobResult(EJobResult jobResult, params Job[] jobs) =>
        WaitForCondition($"Expect job result {jobResult} for {jobs.Select(job => job.Name).JoinWith(",")}",
            () =>
            {
                return jobs.AllMatch(job => job.Result.Value == jobResult);
            }).ExpectWithinSeconds(1);
}
