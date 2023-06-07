using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class JobManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        jobsWithoutParent = new();
    }

    // Static field to be persisted across scenes
    private static List<Job> jobsWithoutParent = new();

    public static JobManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<JobManager>();

    [InjectedInInspector]
    public VisualTreeAsset jobListUi;

    [InjectedInInspector]
    public VisualTreeAsset jobListEntryUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private SceneNavigator sceneNavigator;
    
    private VisualElement jobListElement;
    private Button toggleJobListButton;

    private readonly Dictionary<Job, JobListEntryControl> jobToJobControl = new();
    private readonly HashSet<Job> fadingJobs = new();

    private bool isJobListMinimized;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        CreateJobListUi();
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => OnSceneChanged());

        // CreateDummyJobs();
        // StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(5f, () => CreateDummyJobs()));

        UpdateJobsUi();
    }

    private void Update()
    {
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

    public static Job CreateAndAddJob(string title)
    {
        Job job = new(title);
        Instance.AddJob(job);
        return job;
    }
    
    public void AddJob(Job job)
    {
        if (jobToJobControl.ContainsKey(job))
        {
            // Job was already added
            return;
        }

        if (job.ParentJob == null)
        {
            jobsWithoutParent.Add(job);
        }

        UpdateJobsUi();
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

        jobToJobControl.Remove(job);
        fadingJobs.Remove(job);
    }

    private void UpdateJobsUi()
    {
        jobsWithoutParent.ForEach(job => CreateOrUpdateJobUi(job));
    }

    private void CreateOrUpdateJobUi(Job job)
    {
        if (jobToJobControl.TryGetValue(job, out JobListEntryControl _))
        {
            UpdateJobUi(job);
        }
        else
        {
            CreateJobUi(job);
        }
    }

    private void UpdateJobUi(Job job)
    {
        // Create UI for child jobs that do not have a UI yet
        job.ChildJobs
            .Where(childJob => !jobToJobControl.TryGetValue(childJob, out JobListEntryControl _))
            .ForEach(childJob => CreateJobUi(childJob));
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
            if (this == null)
            {
                // Object was destroyed in the meantime
                return;
            }
            
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

        job.ChildJobs.ForEach(childJob => CreateOrUpdateJobUi(childJob));
    }

    private void CreateJobListUi()
    {
        jobListElement = jobListUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(jobListElement);

        // Remove dummy entries
        jobListElement.Query<VisualElement>("jobListEntry")
            .ForEach(visualElement => visualElement.RemoveFromHierarchy());

        toggleJobListButton = jobListElement.Q<Button>(R.UxmlNames.toggleJobListButton);
        toggleJobListButton.RegisterCallbackButtonTriggered(_ => ToggleJobListMinimized());

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
        if (jobsWithoutParent.IsNullOrEmpty())
        {
            // Move outside of the screen
            jobListElement.style.top = jobListElement.parent.contentRect.height;
            jobListElement.style.right = -jobListElement.contentRect.width;
            return;
        }

        if (isJobListMinimized)
        {
            jobListElement.style.top = jobListElement.parent.contentRect.height - toggleJobListButton.contentRect.height;
            jobListElement.style.right = -(jobListElement.contentRect.width - toggleJobListButton.contentRect.width);
        }
        else
        {
            jobListElement.style.top = jobListElement.parent.contentRect.height - jobListElement.contentRect.height;
            jobListElement.style.right = 0;
        }
    }
    
    private void OnSceneChanged()
    {
        if (jobListElement != null)
        {
            // Move the element to the new scene
            uiDocument.rootVisualElement.Add(jobListElement);
        }
        else
        {
            CreateJobListUi();
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
