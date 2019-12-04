
using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class MicProfile
{
    public string Name { get; set; }
    public Color Color { get; set; } = Colors.crimson;
    public int Amplification { get; set; }
    public int NoiseSuppression { get; set; } = 5;
    public bool IsEnabled { get; set; }

    public bool IsConnected
    {
        get
        {
            return Microphone.devices.Contains(Name);
        }
    }

    public MicProfile()
    {
    }

    public MicProfile(string name)
    {
        this.Name = name;
    }

    public int AmplificationMultiplier()
    {
        switch (Amplification)
        {
            case 6:
                return 2;
            case 12:
                return 4;
            case 18:
                return 8;
            default:
                return 1;
        }
    }
}