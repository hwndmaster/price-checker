import { BaseLoadingTargets } from "@hwndmaster/atom-web-core";

/**
 * Represents the enumeration of views available under the App component.
 */
enum LoadingTargets {
    WholePage = BaseLoadingTargets.WholePage,
    ActiveView = BaseLoadingTargets.ActiveView,

    Products = 100,
    ProductEdit = 101,
    ProductPrices = 102,
    Agents = 200,
    AgentEdit = 201,
}

export default LoadingTargets;
