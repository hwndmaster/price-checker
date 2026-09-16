import React, { useEffect } from "react";
import { useRouteError } from "react-router";
import { reportError } from "@hwndmaster/atom-web-telemetry";

function toError(routeError: unknown): Error {
    if (routeError instanceof Error) {
        return routeError;
    }

    return new Error(typeof routeError === "string" ? routeError : JSON.stringify(routeError));
}

// Named ErrorPage rather than Error: a module-scoped `const Error` shadows the global Error
// constructor, so toError above could not have built one.
const ErrorPage: React.FC = () => {
    const routeError = useRouteError();

    // The router renders this instead of the failed route, which used to be the end of it: the user saw
    // "Something went wrong" and the actual error was never recorded anywhere. Reported once per mount.
    useEffect(() => {
        if (routeError != null) {
            reportError(toError(routeError));
        }
    }, [routeError]);

    return (
        <div style={{ position: "relative", width: "100%", display: "flex", justifyContent: "center", alignItems: "center", flexDirection: "column" }}>
            <h1 style={{ fontSize: "4em" }}>Something went wrong!</h1>
            <span>If it doesn't help, try reloading with purging the local cache using this button:</span>
            <button
                onClick={() => {
                    localStorage.clear();
                    window.location.reload();
                }}
            >
                Purge state
            </button>
        </div>
    );
};

export default ErrorPage;
