using System.Collections.Generic;

public class ListDto<T> : JsonSerializable
{
    public List<T> Items { get; set; } = new();

    public ListDto()
    {
    }

    public ListDto(List<T> items)
    {
        Items = items;
    }
}
