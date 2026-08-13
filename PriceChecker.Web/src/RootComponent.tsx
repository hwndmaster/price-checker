import React from "react";
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import AppRoutes from "./shared/routes";
import Layout from "./components/layout/Layout";
import Products from "./pages/products";
import Agents from "./pages/agents";
import Error from "./pages/error";
import NotFound from "./pages/notFound";

const router = createBrowserRouter([
    {
        path: "/",
        element: <Layout />,
        errorElement: <Error />,
        children: [
            {
                path: AppRoutes.Default.path,
                element: <Products />,
            },
            {
                path: AppRoutes.Agents.path,
                element: <Agents />,
            },
            {
                path: "*",
                element: <NotFound />,
            },
        ],
    },
]);

const RootComponent: React.FC = () => {
    return <RouterProvider router={router} />;
};

export default RootComponent;
