using System.Collections.Generic;

public interface IModSettings : IAutoBoundMod
{
    public List<IModSettingControl> GetModSettingControls();
}
