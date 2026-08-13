import { createAction } from "@reduxjs/toolkit";
import { createActionWithMeta, createActionWithMetaValidatable } from "@hwndmaster/atom-react-redux";
import Agent from "@/models/agent";
import type { AgentSchemaData } from "@/schemas/agentSchema";
import { AgentRef } from "@/models/types";

export const fetchAgents = createAction<void>("agents/fetchAgents");
export const saveAgent = createActionWithMetaValidatable<Agent, AgentRef, AgentSchemaData>("agents/saveAgent");
export const deleteAgent = createActionWithMeta<AgentRef>("agents/deleteAgent");
