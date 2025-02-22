using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class JobListEntryControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    private const float RotationVelocityInDegreesPerSecond = 90f;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.jobPendingIcon)]
    private VisualElement jobPendingIcon;

    [Inject(UxmlName = R.UxmlNames.jobRunningIcon)]
    private VisualElement jobRunningIcon;

    [Inject(UxmlName = R.UxmlNames.jobFinishedIcon)]
    private VisualElement jobFinishedIcon;

    [Inject(UxmlName = R.UxmlNames.jobErrorIcon)]
    private VisualElement jobErrorIcon;

    [Inject(UxmlName = R.UxmlNames.jobNameLabel)]
    private Label jobNameLabel;

    [Inject(UxmlName = R.UxmlNames.jobDurationLabel)]
    private Label jobDurationLabel;

    [Inject(UxmlName = R.UxmlNames.cancelJobButton)]
    private Button cancelJobButton;

    [Inject(UxmlName = R.UxmlNames.jobProgressBar)]
    private ProgressBar jobProgressBar;

    [Inject]
    private IJob job;

    public void OnInjectionFinished()
    {
        jobNameLabel.SetTranslatedText(job.Name);

        UpdateIcons();
        job.Result
            .ObserveOnMainThread()
            .Subscribe(_ =>
            {
                UpdateIcons();
                UpdateCancelJobButton();
            });
        job.Status
            .ObserveOnMainThread()
            .Subscribe(_ =>
            {
                UpdateIcons();
                UpdateCancelJobButton();
            });

        if (job.ParentJob != null)
        {
            VisualElement.AddToClassList("childJob");
        }

        cancelJobButton.RegisterCallbackButtonTriggered(_ =>
        {
            job.Cancel();
        });
        job.IsCancelable
            .ObserveOnMainThread()
            .Subscribe(_ =>
            {
                UpdateCancelJobButton();
            });
        job.IsCanceled
            .ObserveOnMainThread()
            .Subscribe(_ =>
            {
                UpdateCancelJobButton();
            });
        UpdateCancelJobButton();
    }

    private void UpdateCancelJobButton()
    {
        cancelJobButton.SetVisibleByDisplay(job.IsCancelable.Value);
        cancelJobButton.SetEnabled(!job.IsCanceled.Value && job.Result.Value == EJobResult.Pending);
    }

    private void UpdateIcons()
    {
        jobErrorIcon.SetVisibleByDisplay(job.Result.Value == EJobResult.Error);
        jobPendingIcon.SetVisibleByDisplay(job.Result.Value != EJobResult.Error && job.Status.Value == EJobStatus.Pending);
        jobRunningIcon.SetVisibleByDisplay(job.Result.Value != EJobResult.Error && job.Status.Value == EJobStatus.Running);
        jobFinishedIcon.SetVisibleByDisplay(job.Result.Value != EJobResult.Error && job.Status.Value == EJobStatus.Finished);
    }

    public void Update()
    {
        UpdateIconRotation();
        UpdateProgressBar();
        UpdateDurationLabel();
    }

    private void UpdateDurationLabel()
    {
        jobDurationLabel.SetTranslatedText(Translation.Of(TimeUtils.GetMinutesAndSecondsDurationString(job.Progress.CurrentDurationInMillis)));
    }

    private void UpdateProgressBar()
    {
        jobProgressBar.SetVisibleByDisplay(job.Progress.EstimatedTotalDurationInMillis > 0);
        jobProgressBar.value = (int)Math.Floor(job.Progress.EstimatedCurrentProgressInPercent);
    }

    private void UpdateIconRotation()
    {
        float newAngleInDegrees = jobRunningIcon.resolvedStyle.rotate.angle.ToDegrees() +
                                  RotationVelocityInDegreesPerSecond * Time.deltaTime;
        jobRunningIcon.style.rotate = new StyleRotate(new Rotate(new Angle(newAngleInDegrees, AngleUnit.Degree)));
    }

    public void Dispose()
    {
        VisualElement.RemoveFromHierarchy();
    }
}
