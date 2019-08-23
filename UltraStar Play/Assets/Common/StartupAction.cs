using System;
using System.Collections;
using System.Collections.Generic;

public class StartupAction {
    public string Name { get; }
    public List<string> m_dependsOn = new List<string>();

    public void DependsOn(string name) {
        m_dependsOn.Add(name);
    }

    public List<string> Dependencies {
        get {
            return m_dependsOn;
        }
    }

    public Action Run {
        get; internal set;
    }

    public StartupAction(string name, Action action, params string[] dependsOn) {
        Name = name;
        Run = action;
        foreach(var depedency in dependsOn) {
            m_dependsOn.Add(depedency);
        }
    }
}