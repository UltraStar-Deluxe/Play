using System.Collections.Generic;
using UnityEngine;

public static class ColorPalettes
{
    // See https://flatuicolors.com/palette/au
    public static class Aussie
    {
        public static Color greenLandGreen = Colors.CreateColor("#22a6b3");
        public static Color quinceQuelly = Colors.CreateColor("#f0932b");
        public static Color carminePink = Colors.CreateColor("#eb4d4b");
        public static Color spicedNectarine = Colors.CreateColor("#ffbe76");
        public static Color purpleApple = Colors.CreateColor("#6ab04c");
        public static Color helitrope = Colors.CreateColor("#e056fd");
        public static Color turbo = Colors.CreateColor("#f9ca24");

        public static readonly IReadOnlyList<Color> colors = new List<Color>
        {
            purpleApple,
            carminePink,
            spicedNectarine,
            quinceQuelly,
            turbo,
            helitrope,
            greenLandGreen,
        };
    }
}
