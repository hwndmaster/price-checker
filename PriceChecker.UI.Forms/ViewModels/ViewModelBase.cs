using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.Helpers;

namespace Genius.PriceChecker.UI.Forms.ViewModels
{
  public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        protected readonly ConcurrentDictionary<string, object> _propertyBag = new();
        private readonly Dictionary<string, List<ValidationRule>> _validationRules = new();
        private readonly Dictionary<string, List<string>> _errors = new();

        protected bool PropertiesAreInitialized = false;

        public ViewModelBase()
        {
            DetectValidationRules();
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return string.IsNullOrWhiteSpace(propertyName) ?
                _errors.SelectMany(entry => entry.Value) :
                _errors.TryGetValue(propertyName, out List<string> errors) ?
                errors :
                new List<string>();
        }

        protected T GetOrDefault<T>(T defaultValue = default(T), [CallerMemberName] string name = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var result = _propertyBag.GetOrAdd(name, _ => defaultValue);

            return (T) result;
        }

        protected void RaiseAndSetIfChanged<T>(T value, Action<T, T> valueChangedHandler = null, [CallerMemberName] string name = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            object oldValue;
            var isInitial = !_propertyBag.TryGetValue(name, out oldValue);

            if (Equals(oldValue, value))
            {
                if (isInitial)
                {
                    // Initial validation
                    ValidateProperty(name, value);
                }
                return;
            }

            _propertyBag.AddOrUpdate(name, _ => value, (_, __) => value);
            OnPropertyChanged(name);

            if (this is IHasDirtyFlag hasDirtyFlag &&
                name != nameof(IHasDirtyFlag.IsDirty) &&
                PropertiesAreInitialized &&
                (this is not ISelectable || name != nameof(ISelectable.IsSelected)))
            {
                hasDirtyFlag.IsDirty = true;
            }

            ValidateProperty(name, value);

            if (valueChangedHandler != null)
            {
                valueChangedHandler(isInitial ? value : (T)oldValue, value);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ValidateProperty(string propertyName, object value)
        {
            // Clear previous errors of the current property to be validated
            _errors.Remove(propertyName);
            OnErrorsChanged(propertyName);

            if (!_validationRules.TryGetValue(propertyName, out var rules))
            {
                return;
            }

            foreach (var rule in rules)
            {
                AddError(propertyName, rule.Validate(value, CultureInfo.CurrentCulture));
            }
        }

        private void AddError(string propertyName, ValidationResult validationResult)
        {
            if (validationResult.IsValid)
            {
                return;
            }

            if (!_errors.TryGetValue(propertyName, out var errors))
            {
                errors = new List<string>();
                _errors.Add(propertyName, errors);
            }

            errors.Add(validationResult.ErrorContent.ToString());
            OnErrorsChanged(propertyName);
        }

        private void DetectValidationRules()
        {
            var allProperties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in allProperties)
            {
                _validationRules.Add(prop.Name, new List<ValidationRule>());
                foreach (var attr in prop.GetCustomAttributes<ValidationRuleAttribute>())
                {
                    var hasParams = attr.Parameters?.Any() == true;
                    var parameters = hasParams
                        ? new object[] { this }.Concat(attr.Parameters).ToArray()
                        : null;
                    var validationRule = (ValidationRule) (hasParams
                        ? Activator.CreateInstance(attr.ValidationRuleType, parameters)
                        : Activator.CreateInstance(attr.ValidationRuleType));
                    _validationRules[prop.Name].Add(validationRule);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public virtual bool HasErrors => _errors.Any();
    }

    public abstract class ViewModelBase<TViewModel> : ViewModelBase
    {
        public void WhenChanged<TProperty>(Expression<Func<TViewModel, TProperty>> propertyAccessor, Action<TProperty> handler)
        {
            var propName = ExpressionHelpers.GetPropertyName(propertyAccessor);

            PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != propName)
                    return;

                if (!_propertyBag.TryGetValue(propName, out var value))
                    return;

                handler((TProperty) value);
            };
        }
    }
}
