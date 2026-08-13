import type { CommonState } from "@hwndmaster/atom-react-redux";
import * as Agents from "./agents/state";
import * as Products from "./products/state";
import * as Scans from "./scans/state";

interface AppState {
    common: CommonState;
    agents: Agents.default;
    products: Products.default;
    scans: Scans.default;
}

export default AppState;
