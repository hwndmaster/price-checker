using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class FilterContextAttribute : Attribute
    { }
}
