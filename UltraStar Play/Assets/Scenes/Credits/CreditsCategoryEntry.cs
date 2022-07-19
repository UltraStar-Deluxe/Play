using System.Collections.Generic;

public class CreditsCategoryEntry : JsonSerializable
{
    public string Name { get; set; }
    public List<CreditsEntry> Entries { get; set; }
}
