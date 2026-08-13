import { FakeAxios } from "@hwndmaster/atom-testing-utils";
import { setApiAxiosInstance } from "@/api/apiAxios";

const fakeAxios = new FakeAxios(setApiAxiosInstance);

export { fakeAxios };
