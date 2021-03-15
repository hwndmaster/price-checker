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

        public string IconPropertyPath { get; set; }
        public double? FixedSize { get; set; }
    }
}
