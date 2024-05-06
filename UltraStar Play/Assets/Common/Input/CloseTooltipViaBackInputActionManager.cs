using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;

public class CloseTooltipViaBackInputActionManager : AbstractSingletonBehaviour, INeedInjection
{
    public static CloseTooltipViaBackInputActionManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<CloseTooltipViaBackInputActionManager>();

    protected override object GetInstance()
    {
        return Instance;
    }

    public void Start()
    {
        CloseTooltipViaBackAction();
    }

    private void CloseTooltipViaBackAction()
    {
        InputManager.GetInputAction("usplay/back").PerformedAsObservable(101)
            .Subscribe(context =>
            {
                if (TooltipControl.OpenTooltipControls.IsNullOrEmpty())
                {
                    return;
                }
                TooltipControl.OpenTooltipControls.FirstOrDefault().CloseTooltip();
                InputManager.GetInputAction("usplay/back").CancelNotifyForThisFrame();
            })
            .AddTo(gameObject);
    }
}
