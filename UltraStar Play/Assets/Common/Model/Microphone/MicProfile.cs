
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
    public int DelayInMillis { get; set; } = 140;

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
        return Convert.ToInt32(Math.Pow(10d, Amplification / 20d));
    }
}