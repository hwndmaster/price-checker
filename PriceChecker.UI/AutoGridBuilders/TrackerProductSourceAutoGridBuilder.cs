using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.Atom.UI.Forms.Wpf;
using Genius.PriceChecker.UI.Views;

namespace Genius.PriceChecker.UI.AutoGridBuilders;

internal sealed class TrackerProductSourceAutoGridBuilder : IAutoGridBuilder
{
    private readonly IFactory<IAutoGridContextBuilder<TrackerProductSourceViewModel, TrackerProductViewModel>> _contextBuilderFactory;

    public TrackerProductSourceAutoGridBuilder(IFactory<IAutoGridContextBuilder<TrackerProductSourceViewModel, TrackerProductViewModel>> contextBuilderFactory)
    {
        _contextBuilderFactory = contextBuilderFactory.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilderFactory.Create()
            .WithColumns(columns =>
                columns
                    .AddComboBox(x => x.AgentKey, x => x.WithListSource(nameof(TrackerProductViewModel.Agents), fromOwnerContext: true))
                    .AddText(x => x.Argument, x => x.WithAutoWidth())
                    .AddText(x => x.LastPrice, x => x
                        .IsReadOnly()
                        .WithDisplayFormat("€ #,##0.00")
                        .WithStyle(new StylingRecord(HorizontalAlignment: HorizontalAlignment.Right)))
                    .AddText(x => x.Status, x => x
                        .IsReadOnly()
                        .WithIconSource(new IconSourceRecord<TrackerProductSourceViewModel>(vm => vm.StatusIcon, FixedSize: 16d)))
                    .AddCommand(x => x.DeleteCommand, x => x.WithIcon("Trash16"))
                    .AddCommand(x => x.ShowInBrowserCommand, x => x.WithIcon("Web16")));
    }
}
