using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SelectFromListAttribute : Attribute
    {
        public SelectFromListAttribute(string collectionPropertyName, bool fromOwnerContext = false)
        {
            CollectionPropertyName = collectionPropertyName;
            FromOwnerContext = fromOwnerContext;
        }

        public string CollectionPropertyName { get; }
        public bool FromOwnerContext { get; }
    }
}
