public class CompanionClientHandlerAndMicProfile
{
    public ICompanionClientHandler CompanionClientHandler { get; private set; }
    public MicProfile MicProfile { get; private set; }

    public CompanionClientHandlerAndMicProfile(ICompanionClientHandler companionClientHandler, MicProfile micProfile)
    {
        CompanionClientHandler = companionClientHandler;
        MicProfile = micProfile;
    }
}
