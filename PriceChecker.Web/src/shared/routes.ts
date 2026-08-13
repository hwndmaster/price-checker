import { Location as ReactLocation } from "react-router";
import {
    getCurrentRoute as getCurrentRouteBase,
    type RouteDefinition
} from "@hwndmaster/atom-react-core";

const AppRoutes = {
    Default: {
        path: "/",
        defaultParams: {},
    },
    Agents: {
        path: "/agents",
        defaultParams: {},
    },
};

/**
 * Returns the current route definition from the location object, given with @see useLocation().
 * @param location The location object.
 * @returns The route definition associated with the current location.
 */
function getCurrentRoute(location: Location | ReactLocation): RouteDefinition | null {
    return getCurrentRouteBase(AppRoutes as Record<string, RouteDefinition>, location);
}

export default AppRoutes;
export { getCurrentRoute };
