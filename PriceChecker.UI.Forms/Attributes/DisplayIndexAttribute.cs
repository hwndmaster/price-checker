using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DisplayIndexAttribute : Attribute
    {
        public DisplayIndexAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }
}
