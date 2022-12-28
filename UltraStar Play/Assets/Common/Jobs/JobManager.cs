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

    private void Start()
    {
        // CreateDummyJobs();
    }

    void Update()
    {
        jobToJobControl.Values.ForEach(jobListEntryControl => jobListEntryControl.Update());
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

        ShowJobList();
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
        jobListElement.Add(jobListEntryElement);

        JobListEntryControl jobListEntryControl = injector
            .WithRootVisualElement(jobListEntryElement)
            .WithBindingForInstance(job)
            .CreateAndInject<JobListEntryControl>();
        jobToJobControl.Add(job, jobListEntryControl);

        if (job.ParentJob == null)
        {
            job.Result
                .Subscribe(newResult =>
                {
                    if (newResult is EJobResult.Ok or EJobResult.Error)
                    {
                        // This job is done. Thus, fade out, then remove
                        FadeOutThenRemoveJob(job);
                    }
                });
        }

        job.ChildJobs.ForEach(childJob => CreateJobUi(childJob));

        jobListElement.ShowByDisplay();
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
        toggleJobListButton.RegisterCallbackButtonTriggered(() => ToggleJobListVisible());

        jobListElement.HideByDisplay();
    }

    private void ToggleJobListVisible()
    {
        if (jobListElement.ClassListContains("hidden"))
        {
            ShowJobList();
        }
        else
        {
            HideJobList();
        }
    }

    private void ShowJobList()
    {
        jobListElement.RemoveFromClassList("hidden");
        jobListElement.style.bottom = 0;
        jobListElement.style.right = 0;
    }

    private void HideJobList()
    {
        jobListElement.AddToClassList("hidden");
        jobListElement.style.bottom = -(jobListElement.contentRect.height - toggleJobListButton.contentRect.height);
        jobListElement.style.right = -(jobListElement.contentRect.width - toggleJobListButton.contentRect.width);
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
