import { AxiosInstance } from "axios";
import * as api from "./api.generated";

interface ApiClient {
    agents: api.AgentsClient;
    products: api.ProductsClient;
    scans: api.ScansClient;
}
let axiosInstance: AxiosInstance | null = null;
let client: ApiClient | null = null;

const setApiAxiosInstance = (instance: AxiosInstance): void => {
    axiosInstance = instance;
};

const getApiAxiosInstance = (): AxiosInstance => {
    if (axiosInstance == null) {
        throw new Error("The API Axios instance has not been initialized yet.");
    }

    return axiosInstance;
};

const apiClient = (): ApiClient => {
    client ??= {
        agents: new api.AgentsClient("", getApiAxiosInstance()),
        products: new api.ProductsClient("", getApiAxiosInstance()),
        scans: new api.ScansClient("", getApiAxiosInstance()),
    };

    return client;
};

export default apiClient;
export { setApiAxiosInstance, getApiAxiosInstance };
