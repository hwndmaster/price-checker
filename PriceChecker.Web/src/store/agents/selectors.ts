import { createSelector } from "@reduxjs/toolkit";
import Agent from "@/models/agent";
import { AgentRef } from "@/models/types";
import AppState from "@/store/appState";

export const selectAgentById: (state: AppState, agentId: AgentRef) => Agent | undefined = createSelector(
    [(state: AppState, agentId: AgentRef): { agents: Agent[]; agentId: AgentRef } => {
        return { agents: state.agents.agents, agentId };
    }],
    ({ agents, agentId }) => {
        return agents.find((a) => a.id === agentId);
    }
);

export const selectAgentKeys: (state: AppState) => string[] = createSelector(
    [(state: AppState): Agent[] => state.agents.agents],
    (agents) => agents.map((a) => a.key).sort((a, b) => a.localeCompare(b))
);
