public static class ExtendedVoiceIdExtensions
{
    public static bool TryGetVoiceId(this EExtendedVoiceId extendedVoiceId, out EVoiceId voiceId)
    {
        switch (extendedVoiceId)
        {
            case EExtendedVoiceId.P1:
                voiceId = EVoiceId.P1;
                return true;
            case EExtendedVoiceId.P2:
                voiceId = EVoiceId.P2;
                return true;
            default:
                voiceId = EVoiceId.P1;
                return false;
        }
    }
}
