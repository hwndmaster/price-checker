import { expect, afterEach } from "vitest";
import * as matchers from "@testing-library/jest-dom/matchers";
import { cleanup } from "@testing-library/react";

/*
  Vitest offers essential assertion methods to use with expect for asserting values.
  However, it doesn't have the assertion methods for DOM elements such as toBeInTheDocument()
  or toHaveTextContent(). For such methods, we install the @testing-library/jest-dom package
  and extend the expect method from Vitest to include the assertion methods in matchers from this package.
  Ref: https://dev.to/mayashavin/react-component-testing-with-vitest-efficiently-296c
*/
expect.extend(matchers);

afterEach(cleanup);
