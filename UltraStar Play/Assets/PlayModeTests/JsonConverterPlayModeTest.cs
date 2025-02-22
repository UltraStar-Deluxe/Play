using System.Collections;
using CommonOnlineMultiplayer;
using NUnit.Framework;
using Steamworks;
using UnityEngine.TestTools;

// Tests some custom converters that are registered in RuntimeInitializeLoadType.SubsystemRegistration,
// and thus not registered during Play Mode test.
public class JsonConverterPlayModeTest : AbstractPlayModeTest
{
    // TODO: Refactor common code of these tests
    [UnityTest]
    public IEnumerator UnityNetcodeClientIdRoundtripKeepsValue()
    {
        LogAssertUtils.IgnoreFailingMessages();

        Assert.IsTrue(JsonConverter.CustomConverterTypeNames.AnyMatch(it => it.Contains(nameof(UnityNetcodeClientId))),
            $"No custom JSON converter for {nameof(UnityNetcodeClientId)}");

        ulong rawValue = ulong.MaxValue - 1;
        UnityNetcodeClientIdHolder original = new UnityNetcodeClientIdHolder() { UnityNetcodeClientId = new(rawValue), };
        string json = JsonConverter.ToJson(original);
        Assert.IsTrue(json.Contains($"\"{rawValue}\""), $"ulong of {nameof(UnityNetcodeClientId)} was not serialized as string");

        UnityNetcodeClientIdHolder fromJson = JsonConverter.FromJson<UnityNetcodeClientIdHolder>(json);
        Assert.NotNull(fromJson, $"Failed to deserialize {nameof(UnityNetcodeClientIdHolder)}");
        Assert.NotNull(fromJson.UnityNetcodeClientId, $"Failed to deserialize {nameof(UnityNetcodeClientId)}");
        Assert.AreEqual(original.UnityNetcodeClientId.Value, fromJson.UnityNetcodeClientId.Value, $"Failed to deserialize value of {nameof(UnityNetcodeClientId)}");

        yield return null;
    }

    [UnityTest]
    public IEnumerator SteamIdRoundtripKeepsValue()
    {
        LogAssertUtils.IgnoreFailingMessages();

        Assert.IsTrue(JsonConverter.CustomConverterTypeNames.AnyMatch(it => it.Contains(nameof(SteamId))),
            $"No custom JSON converter for {nameof(SteamId)}");

        ulong rawValue = ulong.MaxValue - 1;
        SteamIdHolder original = new SteamIdHolder() { SteamId = new() { Value = rawValue }, };
        string json = JsonConverter.ToJson(original);
        Assert.IsTrue(json.Contains($"\"{rawValue}\""), $"ulong of {nameof(SteamId)} was not serialized as string");

        SteamIdHolder fromJson = JsonConverter.FromJson<SteamIdHolder>(json);
        Assert.NotNull(fromJson, $"Failed to deserialize {nameof(SteamIdHolder)}");
        Assert.NotNull(fromJson.SteamId, $"Failed to deserialize {nameof(SteamId)}");
        Assert.AreEqual(original.SteamId.Value, fromJson.SteamId.Value, $"Failed to deserialize value of {nameof(SteamId)}");

        yield return null;
    }

    private class UnityNetcodeClientIdHolder
    {
        public UnityNetcodeClientId UnityNetcodeClientId { get; set; }
    }

    private class SteamIdHolder
    {
        public SteamId SteamId { get; set; }
    }
}

