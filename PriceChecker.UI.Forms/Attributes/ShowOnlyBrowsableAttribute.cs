using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ShowOnlyBrowsableAttribute : Attribute
    {
        public ShowOnlyBrowsableAttribute(bool onlyBrowsable)
        {
            OnlyBrowsable = onlyBrowsable;
        }

        public bool OnlyBrowsable { get; }
    }
}
