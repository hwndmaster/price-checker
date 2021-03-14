using System.Globalization;
using System.Windows.Controls;

namespace Genius.PriceChecker.UI.Validation
{
    public class ValueCannotBeEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null
                || (value is string valueString && string.IsNullOrWhiteSpace(valueString)))
            {
                return new ValidationResult(false, "Value cannot be empty.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
