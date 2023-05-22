
using System.Threading;
using System.Threading.Tasks;

namespace BasicPitchRunner
{
    public static class BasicPitchRunnerUtils
    {
        public static Task<BasicPitchResult> RunBasicPitch(BasicPitchParameters parameters, CancellationToken cancellationToken)
        {
            return BasicPitchCommandLineRunner.RunBasicPitchAsync(parameters, cancellationToken);
        }
    }
}
