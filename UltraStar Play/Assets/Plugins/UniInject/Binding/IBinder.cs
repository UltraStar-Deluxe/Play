using System.Collections.Generic;

namespace UniInject
{
    public interface IBinder
    {
        List<IBinding> GetBindings();
    }
}