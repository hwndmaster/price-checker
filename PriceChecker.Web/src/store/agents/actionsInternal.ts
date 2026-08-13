import { createAction } from "@reduxjs/toolkit";
import Agent from "@/models/agent";
import { AgentRef } from "@/models/types";

export const setAgents = createAction<Agent[]>("agents/setAgents");
export const setAgent = createAction<Agent>("agents/setAgent");
export const removeAgent = createAction<AgentRef>("agents/removeAgent");
