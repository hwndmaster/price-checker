import { createReducer } from "@reduxjs/toolkit";
import * as actions from "./actionsInternal";
import AgentsState from "./state";

const initialState: AgentsState = {
    agents: [],
};

const agentsReducer = createReducer(initialState, (builder) => {
    builder
        .addCase(actions.setAgents, (state, action) => {
            state.agents = action.payload;
        })
        .addCase(actions.setAgent, (state, action) => {
            const index = state.agents.findIndex((a) => a.id === action.payload.id);
            if (index >= 0) {
                state.agents[index] = action.payload;
            } else {
                state.agents.push(action.payload);
            }
        })
        .addCase(actions.removeAgent, (state, action) => {
            state.agents = state.agents.filter((a) => a.id !== action.payload);
        });
});

export default agentsReducer;
