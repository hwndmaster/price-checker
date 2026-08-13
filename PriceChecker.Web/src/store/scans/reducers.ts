import { createReducer } from "@reduxjs/toolkit";
import * as actions from "./actionsInternal";
import * as publicActions from "./actions";
import ScansState from "./state";

const initialState: ScansState = {
    progress: null,
};

const scansReducer = createReducer(initialState, (builder) => {
    builder
        .addCase(actions.setProgress, (state, action) => {
            state.progress = action.payload;
        })
        .addCase(publicActions.progressChanged, (state, action) => {
            state.progress = action.payload;
        });
});

export default scansReducer;
