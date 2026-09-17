import { createAction } from "@reduxjs/toolkit";
import { createActionWithMeta, createActionWithMetaValidatable } from "@hwndmaster/atom-react-redux";
import Agent from "@/models/agent";
import RecognizedSource from "@/models/recognizedSource";
import type { AgentSchemaData } from "@/schemas/agentSchema";
import { AgentRef } from "@/models/types";

export const fetchAgents = createAction<void>("agents/fetchAgents");
/** Resolves with the names of the available scanning agent handlers. */
export const fetchAgentHandlers = createActionWithMeta<void, string[]>("agents/fetchAgentHandlers");
/** Resolves with the agents that can scan the given product URL, the most specific one first. */
export const recognizeSourceUrl = createActionWithMeta<string, RecognizedSource[]>("agents/recognizeSourceUrl");
export const saveAgent = createActionWithMetaValidatable<Agent, AgentRef, AgentSchemaData>("agents/saveAgent");
export const deleteAgent = createActionWithMeta<AgentRef>("agents/deleteAgent");
