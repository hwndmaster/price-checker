using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Genius.PriceChecker.Infrastructure;

namespace Genius.PriceChecker.UI.Forms.ViewModels
{
    public static class ViewModelExtensions
    {
        public static IDisposable WhenChanged<TViewModel, TProperty>(this TViewModel viewModel, Expression<Func<TViewModel, TProperty>> propertyAccessor, Action<TProperty> handler)
            where TViewModel : ViewModelBase
        {
            var propName = ExpressionHelpers.GetPropertyName(propertyAccessor);

            PropertyChangedEventHandler fn = (_, args) =>
            {
                if (args.PropertyName != propName)
                    return;

                if (!viewModel.TryGetPropertyValue(propName, out var value))
                    return;

                handler((TProperty) value);
            };

            viewModel.PropertyChanged += fn;

            return new DisposableAction(() => viewModel.PropertyChanged -= fn);
        }
    }
}
