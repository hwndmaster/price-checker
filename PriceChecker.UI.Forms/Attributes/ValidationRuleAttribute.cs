using System;

namespace Genius.PriceChecker.UI.Forms.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class ValidationRuleAttribute : Attribute
    {
        public ValidationRuleAttribute(Type validationRuleType, params object[] parameters)
        {
            ValidationRuleType = validationRuleType;
            Parameters = parameters;
        }

        public Type ValidationRuleType { get; }
        public object[] Parameters { get; }
    }
}
