using System.Collections.Generic;

namespace SpleeterSharp
{
    public class SpleeterResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public List<string> WrittenFiles { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
