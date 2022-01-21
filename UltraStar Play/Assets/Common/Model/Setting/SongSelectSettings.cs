using System;
using System.Collections.Generic;

[Serializable]
public class SongSelectSettings
{
    public ESongOrder songOrder = ESongOrder.Artist;
    public List<ESearchProperty> searchProperties = new List<ESearchProperty>
    {
        ESearchProperty.Artist,
        ESearchProperty.Title,
    };
    public string playlistName = "";
}
