using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public static class GradientConfigUtils
{
    private static readonly Regex linearGradientWithoutAngleRegex = new Regex(@"#(?<startColor>[0-9a-fA-F]+), #(?<endColor>[0-9a-fA-F]+)");
    private static readonly Regex linearGradientWithAngleRegex = new Regex(@"(?<angleValue>([0-9]*[.])?[0-9]+())(?<angleUnit>\w+), #(?<startColor>[0-9a-fA-F]+), #(?<endColor>[0-9a-fA-F]+)");
    
    public static string ToCssSyntax(GradientConfig gradientConfig)
    {
        return $"linear-gradient({gradientConfig.angleDegrees}deg, #{Colors.ToHexColor(gradientConfig.startColor)}, #{Colors.ToHexColor(gradientConfig.endColor)})";
    }
    
    public static GradientConfig FromCssSyntax(string cssSyntax)
    {
        string expression = cssSyntax.Replace("linear-gradient", "");
        if (expression.StartsWith("(") && expression.EndsWith(")"))
        {
            expression = expression.Substring(1, expression.Length - 2);
        }
        
        Match match = linearGradientWithAngleRegex.Match(expression);
        if (match.Success)
        {
            string angleValue = match.Groups["angleValue"].Value;
            string angleUnit = match.Groups["angleUnit"].Value;
            string startColor = match.Groups["startColor"].Value;
            string endColor = match.Groups["endColor"].Value;
            float angleDegrees = GetAngleInDegrees(angleValue, angleUnit);
            return new GradientConfig(Colors.CreateColor(startColor), Colors.CreateColor(endColor), angleDegrees);
        }
        match = linearGradientWithoutAngleRegex.Match(expression);
        if (match.Success)
        {
            string startColor = match.Groups["startColor"].Value;
            string endColor = match.Groups["endColor"].Value;
            return new GradientConfig(Colors.CreateColor(startColor), Colors.CreateColor(endColor));
        }
        
        return null;
    }

    private static float GetAngleInDegrees(string angleValue, string angleUnit)
    {
        float angle = float.Parse(angleValue, CultureInfo.InvariantCulture);
        switch (angleUnit)
        {
            case "deg":
                return angle;
            case "grad":
                return angle * 360f / 400f;
            case "rad":
                return angle * 180f / Mathf.PI;
            case "turn":
                return angle * 360f;
            default:
                throw new ArgumentOutOfRangeException(nameof(angleUnit), angleUnit,
                    "Expected angle unit to be one of deg, grad, rad, turn");
        }
    }
}
