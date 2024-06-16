using NUnit.Framework;
using UnityEngine;

public class JsonConverterTest
{
    [Test]
    [TestCase("exampleProperty")]
    [TestCase("ExampleProperty")]
    [TestCase("EXAMPLEPROPERTY")]
    public void ShouldDeserializePrivatePropertySetter(string propertyName)
    {
        string json = "{\"" + propertyName + "\": \"Alice\"}";
        HasPrivatePropertySetter parsed = JsonConverter.FromJson<HasPrivatePropertySetter>(json);
        Assert.AreEqual("Alice", parsed.ExampleProperty);

        string serializedJson = JsonConverter.ToJson(parsed);
        Assert.IsTrue(serializedJson.Contains("ExampleProperty"));
    }

    [Test]
    public void GradientConfigRoundTrip()
    {
        GradientConfig originalGradientConfig = new GradientConfig(Colors.CreateColor("#11223344"), Colors.CreateColor("#55667788"), 32);
        GradientConfigHolder original = new GradientConfigHolder() { gradientConfig = originalGradientConfig };
        string json = JsonConverter.ToJson(original);
        Assert.IsTrue(json.Contains("linear-gradient(32deg, #11223344, #55667788)"));

        GradientConfigHolder parsed = JsonConverter.FromJson<GradientConfigHolder>(json);
        Assert.AreEqual(original.gradientConfig, parsed.gradientConfig);
    }

    [Test]
    public void GradientConfigFromDictionary()
    {
        string json = "{ \"gradientConfig\": { \"startColor\": \"#11223344\", \"endColor\": \"#55667788\", \"angleDegrees\": \"32\" } }";
        GradientConfigHolder parsed = JsonConverter.FromJson<GradientConfigHolder>(json);

        Assert.AreEqual(
            new GradientConfig(Colors.CreateColor("#11223344"), Colors.CreateColor("#55667788"), 32),
            parsed.gradientConfig);
    }

    [Test]
    public void Color32RoundTrip()
    {
        Color32Holder original = new Color32Holder() { color = Colors.CreateColor("#FFEBCD77")};
        string json = JsonConverter.ToJson(original);
        Assert.IsTrue(json.Contains("#FFEBCD77"));

        Color32Holder parsed = JsonConverter.FromJson<Color32Holder>(json);
        Assert.AreEqual(original.color, parsed.color);
    }

    [Test]
    public void Color32FromDictionary()
    {
        string json = "{ \"color\": { \"r\": 255, \"g\": 128,  \"b\": 64, \"a\": 32 } }";
        Color32Holder parsed = JsonConverter.FromJson<Color32Holder>(json);

        Assert.AreEqual(new Color32(255, 128 ,64, 32), parsed.color);
    }

    [Test]
    public void ShouldUseDefaultEnumValueAsFallback()
    {
        ESide2D originalEnum = ESide2D.Right;
        EnumHolder original = new EnumHolder() { side = originalEnum };

        string json = JsonConverter.ToJson(original);
        Assert.IsTrue(json.Contains("Right"));

        EnumHolder parsed = JsonConverter.FromJson<EnumHolder>(json);
        Assert.AreEqual(original.side, parsed.side);

        // when enum string value invalid, should return default enum value
        string jsonWithInvalidEnumValue = json.Replace(originalEnum.ToString(), "InvalidValue");
        EnumHolder parsedInvalid = JsonConverter.FromJson<EnumHolder>(jsonWithInvalidEnumValue);
        Assert.AreEqual(default(ESide2D), parsedInvalid.side);
    }

    private class Color32Holder
    {
        public Color32 color;
    }

    private class EnumHolder
    {
        public ESide2D side;
    }

    private class GradientConfigHolder
    {
        public GradientConfig gradientConfig;
    }

    private class HasPrivatePropertySetter
    {
        public string ExampleProperty { get; private set; }
    }
}
