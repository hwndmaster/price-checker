import { select } from "redux-saga/effects";
import AppState from "./appState";

/**
 * Typed select effect that properly infers the return type of the selector.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function* typedSelect<TResult, TSelector extends (state: AppState, ...args: any[]) => TResult>(
    selector: TSelector,
    ...args: TSelector extends (state: AppState, ...args: infer TParams) => TResult ? TParams : never
): Generator<unknown, TResult, TResult> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-argument
    return yield select(selector, ...(args as any));
}
