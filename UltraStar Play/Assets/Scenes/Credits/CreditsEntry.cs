public class CreditsEntry : JsonSerializable
{
    public string Name { get; set; }
    public string Nickname { get; set; }
    public string Comment { get; set; }
    public string Banner { get; set; }

    public string MainName => !Name.IsNullOrEmpty()
        ? Name
        : Nickname;

    public string MainNameAndNickname => MainName
                                         + ((!Nickname.IsNullOrEmpty() && Nickname != MainName)
                                             ? $" ({Nickname})"
                                             : "");
}
