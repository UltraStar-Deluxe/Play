using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UniRx.Triggers;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class JobManager : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        instance = null;
    }

    private static JobManager instance;
    public static JobManager Instance
    {
        get
        {
            if (instance == null)
            {
                JobManager instanceInScene = GameObjectUtils.FindComponentWithTag<JobManager>("JobManager");
                if (instanceInScene != null)
                {
                    GameObjectUtils.TryInitSingleInstanceWithDontDestroyOnLoad(ref instance, ref instanceInScene);
                }
            }
            return instance;
        }
    }

    [InjectedInInspector]
    public VisualTreeAsset jobListUi;

    [InjectedInInspector]
    public VisualTreeAsset jobListEntryUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    private VisualElement jobListElement;
    private Button toggleJobListButton;

    private readonly List<Job> jobsWithoutParent = new();
    private readonly Dictionary<Job, JobListEntryControl> jobToJobControl = new();

    private readonly HashSet<Job> fadingJobs = new();

    private bool isJobListMinimized;

    // private void Start()
    // {
    //     if (Instance != this)
    //     {
    //         return;
    //     }
    //
    //     CreateDummyJobs();
    //     StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(5f, () => CreateDummyJobs()));
    // }

    void Update()
    {
        if (Instance != this)
        {
            return;
        }

        jobToJobControl.Values.ForEach(jobListEntryControl => jobListEntryControl.Update());

        // Remove completed jobs
        jobsWithoutParent.ForEach(job =>
        {
            if (job.Result.Value is EJobResult.Ok or EJobResult.Error
                && !fadingJobs.Contains(job))
            {
                // This job is done. Thus, fade out, then remove
                FadeOutThenRemoveJob(job);
            }
        });

        UpdateJobListPosition();
    }

    public void AddJob(Job job)
    {
        if (jobToJobControl.ContainsKey(job))
        {
            // Job was already added
            return;
        }

        if (job.ParentJob != null)
        {
            throw new IllegalArgumentException("Can only add top level jobs");
        }

        jobsWithoutParent.Add(job);
        UpdateJobUi();
    }

    private void FadeOutThenRemoveJob(Job job)
    {
        if (!jobToJobControl.TryGetValue(job, out JobListEntryControl jobListEntryControl))
        {
            return;
        }

        LeanTween.value(gameObject, 0f, 1f, 1f)
            .setOnUpdate(factor =>
            {
                jobListEntryControl.VisualElement.style.opacity = 1 - factor;
            })
            .setOnComplete(() =>
            {
                RemoveJob(job);
            });
        job.ChildJobs.ForEach(childJob => FadeOutThenRemoveJob(childJob));

        fadingJobs.Add(job);
    }

    private void RemoveJob(Job job)
    {
        jobsWithoutParent.Remove(job);
        if (jobToJobControl.TryGetValue(job, out JobListEntryControl jobListEntryControl))
        {
            jobListEntryControl.Dispose();
        }

        if (jobsWithoutParent.IsNullOrEmpty())
        {
            jobListElement.HideByDisplay();
        }

        jobToJobControl.Remove(job);
        fadingJobs.Remove(job);
    }

    private void UpdateJobUi()
    {
        jobsWithoutParent
            .Where(job => !jobToJobControl.TryGetValue(job, out JobListEntryControl _))
            .ForEach(job => CreateJobUi(job));
    }

    private void CreateJobUi(Job job)
    {
        VisualElement jobListEntryElement = jobListEntryUi.CloneTree().Children().FirstOrDefault();

        JobListEntryControl jobListEntryControl = injector
            .WithRootVisualElement(jobListEntryElement)
            .WithBindingForInstance(job)
            .CreateAndInject<JobListEntryControl>();
        jobToJobControl.Add(job, jobListEntryControl);

        // Only show this job in the UI if it takes a noticeable amount of time.
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(0.5f, () =>
        {
            if (job.Result.Value is EJobResult.Pending)
            {
                jobListElement.Add(jobListEntryElement);
                jobListElement.ShowByDisplay();

                if (isJobListMinimized)
                {
                    MinimizeJobList();
                }
            }
        }));

        job.ChildJobs.ForEach(childJob => CreateJobUi(childJob));
    }

    public void OnSceneInjectionFinished()
    {
        CreateJobListUi();
    }

    private void CreateJobListUi()
    {
        jobListElement = jobListUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(jobListElement);

        // Remove dummy entries
        jobListElement.Query<VisualElement>("jobListEntry")
            .ForEach(visualElement => visualElement.RemoveFromHierarchy());

        toggleJobListButton = jobListElement.Q<Button>(R.UxmlNames.toggleJobListButton);
        toggleJobListButton.RegisterCallbackButtonTriggered(() => ToggleJobListMinimized());

        jobListElement.HideByDisplay();
        if (isJobListMinimized)
        {
            MinimizeJobList();
        }
    }

    private void ToggleJobListMinimized()
    {
        if (isJobListMinimized)
        {
            MaximizeJobList();
        }
        else
        {
            MinimizeJobList();
        }
    }

    private void MaximizeJobList()
    {
        isJobListMinimized = false;
        jobListElement.RemoveFromClassList("minimized");
    }

    private void MinimizeJobList()
    {
        isJobListMinimized = true;
        jobListElement.AddToClassList("minimized");
    }

    private void UpdateJobListPosition()
    {
        if (isJobListMinimized)
        {
            jobListElement.style.top = jobListElement.parent.contentRect.height - toggleJobListButton.contentRect.height;
            jobListElement.style.right = -(jobListElement.contentRect.width - toggleJobListButton.contentRect.width);        }
        else
        {
            jobListElement.style.top = jobListElement.parent.contentRect.height - jobListElement.contentRect.height;
            jobListElement.style.right = 0;
        }
    }

    private void CreateDummyJobs()
    {
        Subject<bool> testObservable1 = new();
        Subject<bool> testObservable2 = new();

        Job testParentJob = new("Test Parent Job");
        Job testChildJob1 = CreateJobFromObservable("Test Child Job 1", testParentJob, testObservable1);
        CreateJobFromObservable("Test Child Job 2", testParentJob, testObservable2);
        AddJob(testParentJob);
        testChildJob1.SetStatus(EJobStatus.Running);

        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(1f, () => testObservable1.OnNext(true)));
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(10f, () => testObservable2.OnNext(true)));
    }

    public static Job CreateJobFromObservable<T>(string jobName, Job parentJob, IObservable<T> observable)
    {
        Job job = new(jobName, parentJob);
        observable
            .CatchIgnore((Exception ex) => job.SetResult(EJobResult.Error))
            .Subscribe(_ => job.SetStatus(EJobStatus.Finished));
        return job;
    }
}
