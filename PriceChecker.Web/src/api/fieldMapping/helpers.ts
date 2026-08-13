/**
 * Normalizes a backend API field name to a flat form field name.
 * Handles dot notation and array indices (e.g., "foo.bar[0]" → "bar").
 */
export function normalizeApiFieldName(apiFieldName: string): string {
    return apiFieldName.split(".").at(-1)?.split("[")[0] ?? apiFieldName;
}
