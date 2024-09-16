using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Atom.UI.Forms;
using Genius.PriceChecker.Core.Commands;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.UI.Helpers;
using Genius.PriceChecker.UI.ValueConverters;
using ReactiveUI;

namespace Genius.PriceChecker.UI.Views;

public interface ITrackerProductViewModel : IViewModel, ISelectable, IDisposable
{
    void Reconcile(ProductScanStatus status);
    void SetFailed(string errorMessage);

    Guid? Id { get; }
    string Name { get; }
    ProductScanStatus Status { get; set; }
    IActionCommand CommitProductCommand { get; }
    IActionCommand RefreshPriceCommand { get; }
}

[ShowOnlyBrowsable(true)]
internal sealed class TrackerProductViewModel : ViewModelBase, ITrackerProductViewModel
{
    private readonly IAgentQueryService _agentQuery;
    private readonly IProductQueryService _productQuery;
    private readonly IProductStatusProvider _statusProvider;
    private readonly ICommandBus _commandBus;
    private readonly IUserInteraction _ui;
    private readonly IProductInteraction _productInteraction;
    private readonly CompositeDisposable _disposables = new();

    private Product? _product;

    public TrackerProductViewModel(Product? product, IEventBus eventBus,
        ICommandBus commandBus,
        IAgentQueryService agentQuery,
        IProductQueryService productQuery, IProductStatusProvider statusProvider,
        IUserInteraction ui,
        IProductInteraction productInteraction)
    {
        // Dependencies:
        _agentQuery = agentQuery.NotNull();
        _productQuery = productQuery.NotNull();
        _statusProvider = statusProvider.NotNull();
        _commandBus = commandBus.NotNull();
        _product = product.NotNull();
        _ui = ui.NotNull();
        _productInteraction = productInteraction.NotNull();

        // Member initialization:
        // TODO: Fix warning
        InitializeProperties(async () =>
        {
            await RefreshAgentsAsync();
            await RefreshCategoriesAsync();

            if (_product is not null)
            {
                ResetForm();
                Reconcile(_statusProvider.DetermineStatus(_product));
            }
        });

        // Actions:
        CommitProductCommand = new ActionCommand(_ => CommitProductAsync());

        ShowInBrowserCommand = new ActionCommand(async _ =>
            await productInteraction.ShowProductInBrowserAsync(_product?.Lowest?.ProductSource));

        AddSourceCommand = new ActionCommand(_ =>
            Sources.Add(CreateSourceViewModel(null)));

        ResetCommand = new ActionCommand(_ => ResetForm(), _ => _product is not null);

        DropPricesCommand = new ActionCommand(async _ =>
        {
            if (!_ui.AskForConfirmation("Are you sure?", "Prices drop confirmation"))
                return;
            await commandBus.SendAsync(new ProductDropPricesCommand(_product!.Id));
            Reconcile(ProductScanStatus.NotScanned);
        }, _ => _product is not null);

        RefreshPriceCommand = new ActionCommand(async _ =>
        {
            if (Status == ProductScanStatus.Scanning)
                return;
            await commandBus.SendAsync(new ProductEnqueueScanCommand(product!.Id));
        }, _ => _product is not null && Status != ProductScanStatus.Scanning);

        // Subscriptions:
        eventBus.WhenFired<AgentsAffectedEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ =>
                await RefreshAgentsAsync()
            )
            .DisposeWith(_disposables);
        eventBus.WhenFired<ProductsAffectedEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ =>
                await RefreshCategoriesAsync()
            )
            .DisposeWith(_disposables);
    }

    public void Reconcile(ProductScanStatus status)
    {
        Product product = _product.NotNull();
        var previousLowestPrice = LowestPrice;
        LowestPrice = product.Lowest?.Price;
        LowestFoundOn = product.Lowest?.FoundDate;
        RecentPrice = product.MinPrice()?.Price;
        Status = status switch {
            ProductScanStatus.ScannedOk => _statusProvider.DetermineStatus(product),
            _ => status
        };
        LastUpdated = null;

        if (status == ProductScanStatus.ScannedNewLowest
            && LowestPrice.HasValue && previousLowestPrice.HasValue)
        {
            var x = (1 - LowestPrice.Value / previousLowestPrice) * 100;
            StatusText = $"The new price is by {x:0}% less than by previous scan ({LowestPrice.Value:#,##0.00} vs {previousLowestPrice.Value:#,##0.00})";
        }
        else if (Status == ProductScanStatus.ScannedWithErrors)
        {
            StatusText = "One or more source hasn't updated its price. Check the logs.";
        }
        else
        {
            StatusText = string.Empty;
        }

        if (product.Recent.Any())
        {
            LastUpdated = product.Recent.Max(x => x.FoundDate);

            var pricesDict = product.Recent.ToDictionary(x => x.ProductSourceId);
            foreach (var source in Sources)
            {
                if (pricesDict.TryGetValue(source.Id, out var priceValue))
                {
                    source.LastPrice = priceValue.Price;
                    source.Status = priceValue.Status;
                }
                else
                {
                    source.LastPrice = null;
                    source.Status = AgentHandlingStatus.Unknown;
                }
            }
        }
        else
        {
            foreach (var source in Sources)
            {
                source.LastPrice = null;
            }
        }
    }

    public void SetFailed(string errorMessage)
    {
        Status = ProductScanStatus.Failed;
        StatusText = errorMessage;
    }

    private async Task CommitProductAsync()
    {
        if (string.IsNullOrEmpty(Name))
        {
            _ui.ShowWarning("Product name cannot be empty.");
            return;
        }

        var sources = Sources.Select(x => new ProductSource
        {
            Id = x.Id,
                AgentKey = x.AgentKey,
                AgentArgument = x.Argument
        }).ToArray();

        if (_product is null)
        {
            var productId = await _commandBus.SendAsync(new ProductCreateCommand(Name, Category, Description, sources));
            _product = await _productQuery.FindByIdAsync(productId);
        }
        else
        {
            await _commandBus.SendAsync(new ProductUpdateCommand(_product.Id, Name, Category, Description, sources));
        }
    }

    private TrackerProductSourceViewModel CreateSourceViewModel(ProductSource? productSource)
    {
        var lastPrice = productSource is null || _product?.Recent is null
            ? null
            : _product.Recent.FirstOrDefault(x => x.ProductSourceId == productSource.Id)?.Price;
        var vm = new TrackerProductSourceViewModel(_productInteraction, productSource, lastPrice);
        vm.DeleteCommand.Executed
            .Subscribe(_ => Sources.Remove(vm))
            .DisposeWith(_disposables);
        return vm;
    }

    private async Task RefreshAgentsAsync()
    {
        Agents = (await _agentQuery.GetAllAsync()).Select(x => x.Key).ToList();
    }

    private async Task RefreshCategoriesAsync()
    {
        Categories.AppendItems(
            (await _productQuery.GetAllAsync())
                .Where(x => x.Category is not null)
                .Select(x => x.Category!).Distinct());
    }

    private void ResetForm()
    {
        Name = _product?.Name ?? Name;
        Category = _product?.Category;
        Description = _product?.Description;

        var productSourceVms = _product?.Sources.Select(x => CreateSourceViewModel(x))
            ?? new List<TrackerProductSourceViewModel>();
        Sources.ReplaceItems(productSourceVms);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public Guid? Id => _product?.Id;

    public IReadOnlyCollection<string> Agents { get; private set; } = new List<string>();

    public ObservableCollection<TrackerProductSourceViewModel> Sources { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    [Browsable(true)]
    [IconSource(nameof(StatusIcon), fixedSize : 16d, hideText : true)]
    [TooltipSource(nameof(StatusText))]
    [Style(HorizontalAlignment = HorizontalAlignment.Right)]
    public ProductScanStatus Status
    {
        get => GetOrDefault<ProductScanStatus>();
        set => RaiseAndSetIfChanged(value, (_, __) =>
            OnPropertyChanged(nameof(StatusIcon)));
    }

    public BitmapImage? StatusIcon => ResourcesHelper.GetStatusIcon(Status);

    public string StatusText
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    [Browsable(true)]
    [FilterBy]
    public string Name
    {
        get => GetOrDefault<string>();
        set => RaiseAndSetIfChanged(value);
    }

    [GroupBy]
    public string? Category
    {
        get => GetOrDefault<string?>();
        set => RaiseAndSetIfChanged(value);
    }

    public string? Description
    {
        get => GetOrDefault<string?>();
        set => RaiseAndSetIfChanged(value);
    }

    [Browsable(true)]
    [DisplayFormat(DataFormatString = "€ #,##0.00")]
    [Style(HorizontalAlignment = HorizontalAlignment.Right)]
    public decimal? LowestPrice
    {
        get => GetOrDefault<decimal?>();
        set => RaiseAndSetIfChanged(value);
    }

    [Browsable(true)]
    [DisplayFormat(DataFormatString = "€ #,##0.00")]
    [Style(HorizontalAlignment = HorizontalAlignment.Right)]
    public decimal? RecentPrice
    {
        get => GetOrDefault<decimal?>();
        set => RaiseAndSetIfChanged(value);
    }

    [Browsable(true)]
    [ValueConverter(typeof(DateTimeToHumanizedConverter))]
    public DateTime? LowestFoundOn
    {
        get => GetOrDefault<DateTime?>();
        set => RaiseAndSetIfChanged(value);
    }

    [Browsable(true)]
    [ValueConverter(typeof(DateTimeToHumanizedConverter))]
    public DateTime? LastUpdated
    {
        get => GetOrDefault<DateTime?>();
        set => RaiseAndSetIfChanged(value);
    }

    public bool IsSelected
    {
        get => GetOrDefault<bool>();
        set => RaiseAndSetIfChanged(value);
    }

    public IActionCommand CommitProductCommand { get; }

    [Browsable(true)]
    [Icon("Web16")]
    public IActionCommand ShowInBrowserCommand { get; }
    public IActionCommand AddSourceCommand { get; }
    public IActionCommand ResetCommand { get; }
    public IActionCommand DropPricesCommand { get; }

    [Browsable(true)]
    [Icon("Refresh16")]
    public IActionCommand RefreshPriceCommand { get; }
}
