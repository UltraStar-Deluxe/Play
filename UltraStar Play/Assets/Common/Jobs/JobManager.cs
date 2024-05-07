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
    private readonly List<Job> jobsWithoutParent = new();

    public static JobManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<JobManager>();

    public bool AllJobsFinished => AllJobs
        .AllMatch(job => job.Status.Value is EJobStatus.Finished);

    private List<Job> AllJobs => jobsWithoutParent
        .Union(jobToJobControl.Keys)
        .Distinct()
        .ToList();

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

    private bool jobsUiNeedsRefresh;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        CreateJobListUi();
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => OnSceneChanged());

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

        if (jobsUiNeedsRefresh)
        {
            UpdateJobsUi();
        }
    }

    public static Job CreateAndAddJob(Translation title, Job parentJob = null)
    {
        Job job = new(title, parentJob);
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

        jobsUiNeedsRefresh = true;
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
            if (GameObjectUtils.IsDestroyed(this))
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
        float targetTop;
        float targetRight;
        if (jobsWithoutParent.IsNullOrEmpty())
        {
            // Move outside of the screen
            targetTop = (int)Math.Floor(jobListElement.parent.contentRect.height);
            targetRight = -(int)Math.Floor(jobListElement.contentRect.width);
        }
        else if (isJobListMinimized)
        {
            // Move to bottom right corner, only the icon visible
            targetTop = (int)Math.Floor(jobListElement.parent.contentRect.height - toggleJobListButton.contentRect.height);
            targetRight = -(int)Math.Floor(jobListElement.contentRect.width - toggleJobListButton.contentRect.width);
        }
        else
        {
            // Move to bottom right corner, all visible
            targetTop = (int)Math.Floor(jobListElement.parent.contentRect.height - jobListElement.contentRect.height);
            targetRight = 0;
        }

        if (Math.Abs(jobListElement.resolvedStyle.top - targetTop) > 2f)
        {
            jobListElement.style.top = targetTop;
        }
        if (Math.Abs(jobListElement.resolvedStyle.right - targetTop) > 2f)
        {
            jobListElement.style.right = targetRight;
        }

        // Show job list always on top of other elements
        if (!jobsWithoutParent.IsNullOrEmpty()
            || !fadingJobs.IsNullOrEmpty())
        {
            jobListElement.BringToFront();
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

    protected override void OnDestroySingleton()
    {
        Debug.Log("JobManager is destroyed, cancelling remaining jobs");
        jobsWithoutParent.ForEach(job => CancelJob(job, true));
    }

    private void CancelJob(Job job, bool recursive)
    {
        // Cancel child jobs first
        if (recursive
            && !job.ChildJobs.IsNullOrEmpty())
        {
            job.ChildJobs.ForEach(childJob => CancelJob(childJob, recursive));
        }

        if (job.IsCancelable.Value
            && !job.IsCanceled.Value)
        {
            job.Cancel();
        }
    }

    public static Job CreateJobFromObservable<T>(Translation jobName, Job parentJob, IObservable<T> observable)
    {
        Job job = new(jobName, parentJob);
        observable
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Job '{jobName}' failed: {ex.Message}");
                job.SetResult(EJobResult.Error);
                throw ex;
            })
            .Subscribe(_ => job.SetStatus(EJobStatus.Finished));
        return job;
    }
}
