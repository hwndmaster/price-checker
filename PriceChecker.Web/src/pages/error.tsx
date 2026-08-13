import React from "react";

const Error: React.FC = () => {
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

export default Error;
