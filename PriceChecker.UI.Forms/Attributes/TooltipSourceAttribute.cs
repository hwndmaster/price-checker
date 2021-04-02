using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class TooltipSourceAttribute : Attribute
    {
        public TooltipSourceAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
