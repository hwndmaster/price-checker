import apiClient from "@/api/apiAxios";

/**
 * Fetches the list of the available scanning agent handlers.
 * The list is static per application version, so no store round-trip is involved.
 */
export async function fetchAgentHandlers(): Promise<string[]> {
    const response = await apiClient().agents.handlers();
    return response.data;
}
