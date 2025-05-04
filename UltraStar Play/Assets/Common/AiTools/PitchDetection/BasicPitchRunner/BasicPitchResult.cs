using System.Collections.Generic;

namespace BasicPitchRunner
{
    public class BasicPitchResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public List<string> WrittenFiles { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
