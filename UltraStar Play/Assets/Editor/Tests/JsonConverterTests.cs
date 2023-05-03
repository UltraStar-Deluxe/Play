using System.Collections.Generic;
using NUnit.Framework;
using UniRx;
using UnityEngine;

public class JsonConverterTests
{
    [Test]
    public void FillReactivePropertyReferenceTest()
    {
        ReactivePropertyUserTypeHolder original = CreateReactivePropertyUserTypeHolder();

        PlayerProfile observedPlayerProfileValue = null;
        original.ReactivePropertyUserType.Subscribe(newValue => observedPlayerProfileValue = newValue);
        
        ReactiveProperty<PlayerProfile> originalReactiveProperty = original.ReactivePropertyUserType;
        PlayerProfile originalPlayerProfile = original.ReactivePropertyUserType.Value;
        string json = JsonConverter.ToJson(original);
        
        JsonConverter.FillFromJson(json, original);
        ReactiveProperty<PlayerProfile> filledReactiveProperty = original.ReactivePropertyUserType;

        Assert.AreSame(originalReactiveProperty, filledReactiveProperty);
        Assert.AreEqual(observedPlayerProfileValue.Name, originalPlayerProfile.Name);
    }

    [Test]
    public void SerializeReactivePropertyString()
    {
        ReactivePropertyStringHolder original = CreateReactivePropertyStringHolder();
        string json = JsonConverter.ToJson(original);
        // Json contains the value, without serializing the whole property to json.
        Assert.IsTrue(json.ToLowerInvariant().Contains("some string")
            && !json.ToLowerInvariant().Contains("value"),
            "string serialized to JSON with fields instead of only the value");
    }
    
    [Test]
    public void RoundTripReactivePropertyString()
    {
        ReactivePropertyStringHolder original = CreateReactivePropertyStringHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyStringHolder deserialized = JsonConverter.FromJson<ReactivePropertyStringHolder>(json);
        Assert.AreEqual(original.ReactivePropertyString.Value, deserialized.ReactivePropertyString.Value);
    }

    // [Test]
    // public void RoundTripNullValue()
    // {
    //     ReactivePropertyStringHolder original = CreateReactivePropertyStringHolder();
    //     original.ReactivePropertyString = null;
    //     string json = JsonConverter.ToJson(original);
    //     ReactivePropertyStringHolder deserialized = JsonConverter.FromJson<ReactivePropertyStringHolder>(json);
    //     // TODO: A null value always removes the ReactiveProperty instead of setting its value to null.
    //     Assert.IsNotNull(deserialized.ReactivePropertyString, "Deserializing null value has removed the ReactiveProperty");
    //     Assert.IsNull(deserialized.ReactivePropertyString.Value, "Deserialized null value but the ReactiveProperty has a value");
    // }
    
    [Test]
    public void SerializeReactivePropertyUserType()
    {
        ReactivePropertyUserTypeHolder original = CreateReactivePropertyUserTypeHolder();
        string json = JsonConverter.ToJson(original);
        // Json contains the value, without serializing the whole property to json.
        Assert.IsTrue(json.ToLowerInvariant().Contains("\"name\":\"dummy player profile\"")
                      && !json.ToLowerInvariant().Contains("value"),
            "user type serialized to JSON with fields instead of only the value");
    }
    
    [Test]
    public void RoundTripReactivePropertyUserType()
    {
        ReactivePropertyUserTypeHolder original = CreateReactivePropertyUserTypeHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyUserTypeHolder deserialized = JsonConverter.FromJson<ReactivePropertyUserTypeHolder>(json);
        Assert.NotNull(deserialized.ReactivePropertyUserType.Value, "deserialized user type is null");
        Assert.AreEqual(
            original.ReactivePropertyUserType.Value.Name,
            deserialized.ReactivePropertyUserType.Value.Name);
    }
    
    [Test]
    public void SerializeReactivePropertyEnum()
    {
        ReactivePropertyEnumHolder original = CreateReactivePropertyEnumHolder();
        string json = JsonConverter.ToJson(original);
        // Json contains the value, without serializing the whole property to json.
        Assert.IsTrue(json.ToLowerInvariant().Contains("windowed")
            && !json.ToLowerInvariant().Contains("value"),
            "enum serialized to JSON with fields instead of only the value");
    }
    
    [Test]
    public void RoundTripReactivePropertyEnum()
    {
        ReactivePropertyEnumHolder original = CreateReactivePropertyEnumHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyEnumHolder deserialized = JsonConverter.FromJson<ReactivePropertyEnumHolder>(json);
        Assert.NotNull(deserialized.ReactivePropertyEnum.Value, "deserialized enum is null");
        Assert.AreEqual(deserialized.ReactivePropertyEnum.Value, FullScreenMode.Windowed);
    }
    
    [Test]
    public void RoundTripReactivePropertyInt()
    {
        ReactivePropertyIntHolder original = CreateReactivePropertyIntHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyIntHolder deserialized = JsonConverter.FromJson<ReactivePropertyIntHolder>(json);
        Assert.AreEqual(deserialized.ReactivePropertyInt.Value, 42);
    }
    
    [Test]
    public void RoundTripReactivePropertyFloat()
    {
        ReactivePropertyFloatHolder original = CreateReactivePropertyFloatHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyFloatHolder deserialized = JsonConverter.FromJson<ReactivePropertyFloatHolder>(json);
        Assert.AreEqual(deserialized.ReactivePropertyFloat.Value, 42.5f);
    }
    
    [Test]
    public void SerializeReactivePropertyList()
    {
        ReactivePropertyListHolder original = CreateReactivePropertyListHolder();
        string json = JsonConverter.ToJson(original);
        // Json contains the value, without serializing the whole property to json.
        Assert.IsTrue(json.ToLowerInvariant().Contains("\"name\":\"dummy player profile\"")
            && !json.ToLowerInvariant().Contains("value"),
            "list serialized to JSON with fields instead of only the value");
    }
    
    [Test]
    public void RoundTripReactivePropertyList()
    {
        ReactivePropertyListHolder original = CreateReactivePropertyListHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyListHolder deserialized = JsonConverter.FromJson<ReactivePropertyListHolder>(json);
        Assert.NotNull(deserialized.ReactivePropertyList.Value, "deserialized list is null");
        Assert.IsNotEmpty(deserialized.ReactivePropertyList.Value, "deserialized list is empty");
        Assert.AreEqual(
            original.ReactivePropertyList.Value.Count,
            deserialized.ReactivePropertyList.Value.Count, "deserialized list length mismatch");
        for (var i = 0; i < original.ReactivePropertyList.Value.Count; i++)
        {
            Assert.AreEqual(
                original.ReactivePropertyList.Value[i].Name,
                deserialized.ReactivePropertyList.Value[i].Name);
        }
    }
    
    [Test]
    public void RoundTripReactivePropertyDictionary()
    {
        ReactivePropertyDictionaryHolder original = CreateReactivePropertyDictionaryHolder();
        string json = JsonConverter.ToJson(original);
        ReactivePropertyDictionaryHolder deserialized = JsonConverter.FromJson<ReactivePropertyDictionaryHolder>(json);
        Assert.NotNull(deserialized.ReactivePropertyDictionary.Value, "deserialized dictionary is null");
        Assert.AreEqual(
            original.ReactivePropertyDictionary.Value.Count,
            deserialized.ReactivePropertyDictionary.Value.Count, "deserialized dictionary length mismatch");
        foreach (string key in original.ReactivePropertyDictionary.Value.Keys)
        {
            Assert.AreEqual(
                original.ReactivePropertyDictionary.Value[key],
                deserialized.ReactivePropertyDictionary.Value[key]);
        }
    }
    
    private ReactivePropertyStringHolder CreateReactivePropertyStringHolder()
    {
        ReactivePropertyStringHolder result = new();
        result.ReactivePropertyString.Value = "Some string";
        return result;
    }
    
    private ReactivePropertyListHolder CreateReactivePropertyListHolder()
    {
        ReactivePropertyListHolder result = new();
        result.ReactivePropertyList.Value = new List<PlayerProfile>
        {
            new PlayerProfile("Dummy Player Profile", EDifficulty.Medium),
        };
        return result;
    }
    
    private ReactivePropertyUserTypeHolder CreateReactivePropertyUserTypeHolder()
    {
        ReactivePropertyUserTypeHolder result = new();
        result.ReactivePropertyUserType.Value = new PlayerProfile("Dummy Player Profile", EDifficulty.Medium);
        return result;
    }
    
    private ReactivePropertyDictionaryHolder CreateReactivePropertyDictionaryHolder()
    {
        ReactivePropertyDictionaryHolder result = new();
        result.ReactivePropertyDictionary.Value = new()
        {
            { "one", 1 },
            { "two", 2 },
        };
        return result;
    }
    
    private ReactivePropertyEnumHolder CreateReactivePropertyEnumHolder()
    {
        ReactivePropertyEnumHolder result = new();
        result.ReactivePropertyEnum.Value = FullScreenMode.Windowed;
        return result;
    }
    
    private ReactivePropertyIntHolder CreateReactivePropertyIntHolder()
    {
        ReactivePropertyIntHolder result = new();
        result.ReactivePropertyInt.Value = 42;
        return result;
    }
    
    private ReactivePropertyFloatHolder CreateReactivePropertyFloatHolder()
    {
        ReactivePropertyFloatHolder result = new();
        result.ReactivePropertyFloat.Value = 42.5f;
        return result;
    }
    
    private class ReactivePropertyStringHolder
    {
        public ReactiveProperty<string> ReactivePropertyString { get; set; } = new();
    }
    
    private class ReactivePropertyListHolder
    {
        public ReactiveProperty<List<PlayerProfile>> ReactivePropertyList { get; set; } = new();
    }
    
    private class ReactivePropertyDictionaryHolder
    {
        public ReactiveProperty<Dictionary<string, int>> ReactivePropertyDictionary { get; set; } = new();
    }
    
    private class ReactivePropertyUserTypeHolder
    {
        public ReactiveProperty<PlayerProfile> ReactivePropertyUserType { get; set; } = new();
    }
    
    private class ReactivePropertyEnumHolder
    {
        public ReactiveProperty<FullScreenMode> ReactivePropertyEnum { get; set; } = new();
    }
    
    private class ReactivePropertyIntHolder
    {
        public ReactiveProperty<int> ReactivePropertyInt { get; set; } = new();
    }
    
    private class ReactivePropertyFloatHolder
    {
        public ReactiveProperty<float> ReactivePropertyFloat { get; set; } = new();
    }
}
