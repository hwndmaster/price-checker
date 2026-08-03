using Genius.Atom.UI.Forms.Controls.AutoGrid.Builders;
using Genius.PriceChecker.UI.Views;

namespace Genius.PriceChecker.UI.AutoGridBuilders;

internal sealed class AgentAutoGridBuilder : IAutoGridBuilder
{
    private readonly IFactory<IAutoGridContextBuilder<AgentViewModel, AgentsViewModel>> _contextBuilderFactory;

    public AgentAutoGridBuilder(IFactory<IAutoGridContextBuilder<AgentViewModel, AgentsViewModel>> contextBuilderFactory)
    {
        _contextBuilderFactory = contextBuilderFactory.NotNull();
    }

    public IAutoGridContextBuilder Build()
    {
        return _contextBuilderFactory.Create()
            .WithColumns(columns =>
                columns
                    .AddText(x => x.Key)
                    .AddText(x => x.Url, x => x.WithAutoWidth())
                    .AddComboBox(x => x.Handler, x => x.WithListSource(nameof(AgentsViewModel.AgentHandlers), fromOwnerContext: true))
                    .AddText(x => x.PricePattern, x => x.WithAutoWidth())
                    .AddText(x => x.DecimalDelimiter));
    }
}
