import React from "react";
import { useNavigate } from "react-router";
import { goTo } from "@hwndmaster/atom-react-core";
import AppRoutes from "../shared/routes";

const NotFound: React.FC = () => {
    const navigate = useNavigate();

    const redirectToHomePage = async (): Promise<void> => {
        await goTo(navigate, AppRoutes.Default);
    };

    return (
        <div style={{ position: "relative", width: "100%", display: "flex", justifyContent: "center", alignItems: "center", flexDirection: "column" }}>
            <h1 style={{ fontSize: "4em" }}>Oops 404!</h1>
            <span style={{ cursor: "pointer" }} onClick={() => void redirectToHomePage()}>
                Homepage
            </span>
        </div>
    );
};

export default NotFound;
