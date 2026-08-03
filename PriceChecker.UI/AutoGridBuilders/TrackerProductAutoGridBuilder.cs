using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.PriceChecker.UI.ValueConverters;
using Genius.PriceChecker.UI.Views;

namespace Genius.PriceChecker.UI.AutoGridBuilders;

internal sealed class TrackerProductAutoGridBuilder : IAutoGridBuilder
{
    private const string PriceDisplayFormat = "€ #,##0.00";

    private readonly IFactory<IAutoGridContextBuilder<TrackerProductViewModel, TrackerViewModel>> _contextBuilderFactory;

    public TrackerProductAutoGridBuilder(IFactory<IAutoGridContextBuilder<TrackerProductViewModel, TrackerViewModel>> contextBuilderFactory)
    {
        _contextBuilderFactory = contextBuilderFactory.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilderFactory.Create()
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Status, x => x
                        .WithIconSource(new IconSourceRecord<TrackerProductViewModel>(vm => vm.StatusIcon, FixedSize: 16d, HideText: true))
                        .WithToolTipPath(vm => vm.StatusText)
                        .WithStyle(new StylingRecord(HorizontalAlignment: HorizontalAlignment.Right)))
                    .AddText(x => x.Name, x => x.Filterable())
                    .AddText(x => x.Category, x => x.IsGrouped())
                    .AddText(x => x.LowestPrice, x => x
                        .WithDisplayFormat(PriceDisplayFormat)
                        .WithStyle(new StylingRecord(HorizontalAlignment: HorizontalAlignment.Right)))
                    .AddText(x => x.RecentPrice, x => x
                        .WithDisplayFormat(PriceDisplayFormat)
                        .WithStyle(new StylingRecord(HorizontalAlignment: HorizontalAlignment.Right)))
                    .AddText(x => x.LowestFoundOn, x => x.WithValueConverter<DateTimeToHumanizedConverter>())
                    .AddText(x => x.LastUpdated, x => x.WithValueConverter<DateTimeToHumanizedConverter>())
                    .AddCommand(x => x.ShowInBrowserCommand, x => x.WithIcon("Web16"))
                    .AddCommand(x => x.RefreshPriceCommand, x => x.WithIcon("Refresh16")));
    }
}
