using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DialogManager : AbstractSingletonBehaviour, INeedInjection, IBinder
{
    public static DialogManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<DialogManager>();

    [InjectedInInspector]
    public VisualTreeAsset messageDialogUi;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    protected override object GetInstance()
    {
        return Instance;
    }

    public MessageDialogControl CreateDialogControl(Translation dialogTitle)
    {
        VisualElement dialogVisualElement = messageDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialogVisualElement);
        dialogVisualElement.AddToClassList("wordWrap");

        MessageDialogControl dialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<MessageDialogControl>();
        dialogControl.Title = dialogTitle;

        return dialogControl;
    }

    public MessageDialogControl CreateErrorInfoDialogControl(
        Translation dialogTitle,
        Translation dialogMessage,
        Translation errorMessage,
        Translation closeButtonText = default)
    {
        MessageDialogControl messageDialogControl = CreateInfoDialogControl(dialogTitle, dialogMessage, closeButtonText);

        // Add accordion item to show error message.
        if (errorMessage.Value.IsNullOrEmpty())
        {
            return messageDialogControl;
        }

        AccordionItem accordionItem = new AccordionItem();
        accordionItem.SetTranslatedTitle(Translation.Get(R.Messages.common_details));
        Label errorMessageLabel = new();
        errorMessageLabel.SetTranslatedText(errorMessage);
        accordionItem.Add(errorMessageLabel);
        accordionItem.HideAccordionContent();
        messageDialogControl.AddVisualElement(accordionItem);

        return messageDialogControl;
    }

    public MessageDialogControl CreateInfoDialogControl(
        Translation dialogTitle,
        Translation dialogMessage,
        Translation closeButtonText = default)
    {
        MessageDialogControl messageDialogControl = CreateDialogControl(dialogTitle);
        messageDialogControl.Message = dialogMessage;

        closeButtonText = !closeButtonText.Value.IsNullOrEmpty()
            ? closeButtonText
            : Translation.Get(R.Messages.action_close);
        messageDialogControl.AddButton(closeButtonText, evt =>
        {
            messageDialogControl.CloseDialog();
        });
        return messageDialogControl;
    }

    public MessageDialogControl CreateConfirmationDialogControl(
        Translation dialogTitle,
        Translation dialogMessage,
        Translation confirmButtonText,
        Action<EventBase> onConfirm,
        Translation cancelButtonText = default,
        Action<EventBase> onCancel = null)
    {
        MessageDialogControl messageDialogControl = CreateDialogControl(dialogTitle);
        messageDialogControl.Message = dialogMessage;
        messageDialogControl.AddButton(confirmButtonText, evt =>
        {
            messageDialogControl.CloseDialog();
            onConfirm?.Invoke(evt);
        });

        cancelButtonText = !cancelButtonText.Value.IsNullOrEmpty()
            ? cancelButtonText
            : Translation.Get(R.Messages.action_cancel);
        messageDialogControl.AddButton(cancelButtonText, evt =>
        {
            messageDialogControl.CloseDialog();
            onCancel?.Invoke(evt);
        });
        return messageDialogControl;
    }

    public MessageDialogControl CreateHelpDialogControl(
        Translation dialogTitle,
        Dictionary<string, string> titleToContentMap)
    {
        VisualElement dialogVisualElement = messageDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialogVisualElement);
        dialogVisualElement.AddToClassList("wordWrap");

        MessageDialogControl dialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<MessageDialogControl>();
        dialogControl.Title = dialogTitle;

        AccordionGroup accordionGroup = new();
        dialogControl.AddVisualElement(accordionGroup);

        void AddChapter(string title, string content)
        {
            AccordionItem accordionItem = new(title);
            accordionItem.Add(new Label(content));
            accordionGroup.Add(accordionItem);
        }

        titleToContentMap.ForEach(entry => AddChapter(entry.Key, entry.Value));

        return dialogControl;
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.Bind(nameof(messageDialogUi)).ToExistingInstance(messageDialogUi);
        return bb.GetBindings();
    }
}
