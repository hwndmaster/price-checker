import React, { useMemo } from "react";
import { Outlet, useLocation, useNavigate } from "react-router";
import { goTo } from "@hwndmaster/atom-react-core";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { Menubar, type MenuItem } from "@/primereact";
import AppRoutes, { getCurrentRoute } from "@/shared/routes";
import LoadingTargets from "@/shared/loadingTargets";
import ScanProgressBar from "@/components/scanProgress/ScanProgressBar";
import "./layout.module.scss";

const routeTitles = new Map([
    [AppRoutes.Default.path, "Products"],
    [AppRoutes.Agents.path, "Agents"],
]);

const Layout: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();

    const menuItems: MenuItem[] = [
        {
            label: "Products",
            icon: "pi pi-shopping-cart",
            command: () => void goTo(navigate, AppRoutes.Default),
        },
        {
            label: "Agents",
            icon: "pi pi-globe",
            command: () => void goTo(navigate, AppRoutes.Agents),
        },
    ];

    const pageTitle = useMemo(() => {
        const defaultTitle = "Price Checker";
        const currentRoute = getCurrentRoute(location);
        if (currentRoute == null) return defaultTitle;
        return routeTitles.get(currentRoute.path) ?? defaultTitle;
    }, [location]);

    const end = (
        <div className="flex align-items-center gap-2">
            <ScanProgressBar />
            <span className="font-bold">{pageTitle}</span>
        </div>
    );

    return (
        <div className="layout-container">
            <Menubar model={menuItems} end={end} className="layout-menubar" />

            <div className="layout-content">
                <LoadingSpinner target={LoadingTargets.ActiveView}>
                    <Outlet />
                </LoadingSpinner>
            </div>
        </div>
    );
};

export default Layout;
