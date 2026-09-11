using System.Threading.Tasks;
using UnityEngine;

public static class SongEditorAudioSeparationUtils
{
    public static async Awaitable AskToPerformAudioSeparation(
        SongMeta songMeta,
        AudioSeparationManager audioSeparationManager,
        DialogManager dialogManager)
    {
        if (SongMetaUtils.VocalsAudioResourceExists(songMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(songMeta))
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            dialogManager.CreateConfirmationDialogControl(
                Translation.Get(R.Messages.songEditor_audioSeparation_confirmationDialog_title),
                Translation.Get(R.Messages.songEditor_audioSeparation_confirmationDialog_message),
                Translation.Get(R.Messages.common_ok),
                _ => tcs.SetResult(true),
                Translation.Get(R.Messages.action_cancel),
                _ => tcs.SetResult(false));

            if (!await tcs.Task)
            {
                return;
            }
        }

        await audioSeparationManager.ProcessSongMetaJob(songMeta, true).GetResultAsync();
    }
}
