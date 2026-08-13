import { AxiosInstanceFactory } from "@hwndmaster/atom-api-core";
import { ApiUrl } from "@/shared/constants";
import { setApiAxiosInstance } from "./apiAxios";

/**
 * Sets up Axios instances.
 */
export function setupAxiosInstances(): void {
    setApiAxiosInstance(new AxiosInstanceFactory(ApiUrl, "Api").build());
}
