using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    public sealed class IconSourceAttribute : Attribute
    {
        public IconSourceAttribute(string iconPropertyPath)
        {
            IconPropertyPath = iconPropertyPath;
        }

        public IconSourceAttribute(string iconPropertyPath, double fixedSize)
            : this (iconPropertyPath)
        {
            FixedSize = fixedSize;
        }

        public IconSourceAttribute(string iconPropertyPath, double fixedSize, bool hideText)
            : this (iconPropertyPath, fixedSize)
        {
            HideText = hideText;
        }

        public string IconPropertyPath { get; }
        public double? FixedSize { get; }
        public bool HideText { get; } = false;
    }
}
