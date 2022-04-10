using UnityEngine;

public interface ISettings
{
    public SystemLanguage Language { get; set; }
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; }
}
