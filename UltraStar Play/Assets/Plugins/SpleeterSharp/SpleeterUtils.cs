using System.Threading;
using System.Threading.Tasks;

namespace SpleeterSharp
{
    public static class SpleeterUtils
    {
        public static Task<SpleeterResult> SplitAsync(SpleeterParameters spleeterParameters, CancellationToken cancellationToken)
        {
            return SpleeterCommandLineRunner.SplitAsync(spleeterParameters, cancellationToken);
        }
    }
}
