using System;

// What the rules of Memory Cards (the folder above) use of UnityEngine, for compiling them outside Unity: into the
// server module (Server/StdbModule.csproj) and into the model tests (Tests/MemoryCards.Model.Tests.csproj). Unity
// skips this folder (the ~), so inside the editor the real UnityEngine is used. Keep it to what the model needs.
namespace UnityEngine
{
    public static class Mathf
    {
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);

        public static int FloorToInt(float value) => (int)Math.Floor(value);

        public static int RoundToInt(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        public static int Max(int a, int b) => Math.Max(a, b);

        public static float Max(float a, float b) => Math.Max(a, b);

        public static int Min(int a, int b) => Math.Min(a, b);

        public static float Min(float a, float b) => Math.Min(a, b);

        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

        public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }

        public string Tooltip { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header)
        {
            Header = header;
        }

        public string Header { get; }
    }
}
