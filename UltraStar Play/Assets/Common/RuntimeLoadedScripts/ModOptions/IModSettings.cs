using System.Collections.Generic;

public interface IModSettings : IAutoBoundRuntimeLoadedScript
{
    public List<IModSettingControl> GetModSettingControls();
}
