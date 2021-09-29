using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Genius.Atom.UI.Forms;

namespace Genius.PriceChecker.UI.Validation
{
    public class MustBeUniqueValidationRule : ValidationRule
    {
        private readonly ViewModelBase _viewModel;
        private readonly string _currentCollectionPropertyName;

        public MustBeUniqueValidationRule(ViewModelBase viewModel, string uniqueCollectionPropertyName)
        {
            _viewModel = viewModel;
            _currentCollectionPropertyName = uniqueCollectionPropertyName;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is not string valueString)
            {
                return ValidationResult.ValidResult;
            }

            var uniqueCollection = _viewModel.GetType()
                .GetProperty(_currentCollectionPropertyName)
                .GetValue(_viewModel) as IEnumerable<string>;

            var count = uniqueCollection.Count(x => x == valueString);

            if (count > 1)
            {
                return new ValidationResult(false, "Value must be unique.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
