using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Genius.PriceChecker.Infrastructure;

namespace Genius.PriceChecker.UI.Forms.ViewModels
{
    public static class ViewModelExtensions
    {
        public static IDisposable WhenChanged<TViewModel, TProperty>(this TViewModel viewModel, Expression<Func<TViewModel, TProperty>> propertyAccessor, Action<TProperty> handler)
            where TViewModel : IViewModel
        {
            var propName = ExpressionHelpers.GetPropertyName(propertyAccessor);

            return WhenChanged(viewModel, propName, handler);
        }

        public static IDisposable WhenChanged<TProperty>(this IViewModel viewModel, string propertyName, Action<TProperty> handler)
        {
            PropertyChangedEventHandler fn = (_, args) =>
            {
                if (args.PropertyName != propertyName)
                    return;

                if (!viewModel.TryGetPropertyValue(propertyName, out var value))
                    return;

                handler((TProperty) value);
            };

            viewModel.PropertyChanged += fn;

            return new DisposableAction(() => viewModel.PropertyChanged -= fn);
        }
    }
}
