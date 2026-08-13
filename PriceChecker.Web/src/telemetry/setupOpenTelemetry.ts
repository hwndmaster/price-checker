import { SpanStatusCode, type Span as ApiSpan } from "@opentelemetry/api";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-proto";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { XMLHttpRequestInstrumentation } from "@opentelemetry/instrumentation-xml-http-request";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { BatchSpanProcessor, WebTracerProvider, type Span, type SpanProcessor } from "@opentelemetry/sdk-trace-web";
import { ATTR_DEPLOYMENT_ENVIRONMENT_NAME, ATTR_SERVICE_NAME } from "@opentelemetry/semantic-conventions";

const DefaultTraceExportPath = "/otlp/v1/traces";
const DefaultServiceName = "pricechecker-web";
const OTelDisabledSessionStorageKey = "pricechecker:otel-disabled";
let isOpenTelemetryInitialized = false;

function getNonEmptyValue(value: string | undefined): string | undefined {
    if (value == null) {
        return undefined;
    }

    const trimmedValue = value.trim();
    return trimmedValue.length > 0 ? trimmedValue : undefined;
}

function getTraceExporterUrl(): string {
    const configuredEndpoint = getNonEmptyValue(import.meta.env.VITE_OTEL_EXPORTER_OTLP_ENDPOINT);
    if (configuredEndpoint != null) {
        return configuredEndpoint;
    }

    return `${window.location.origin}${DefaultTraceExportPath}`;
}

function getSessionStorage(): Storage | undefined {
    try {
        return window.sessionStorage;
    } catch {
        return undefined;
    }
}

function isOpenTelemetryDisabledForSession(): boolean {
    return getSessionStorage()?.getItem(OTelDisabledSessionStorageKey) === "1";
}

function disableOpenTelemetryForSession(): void {
    getSessionStorage()?.setItem(OTelDisabledSessionStorageKey, "1");
}

function isSuccessfulExportResult(result: { code: number; error?: unknown }): boolean {
    return result.error == null && result.code === 0;
}

function getStringAttribute(span: Span, ...keys: string[]): string | undefined {
    for (const key of keys) {
        const value = span.attributes[key];
        if (typeof value === "string" && value !== "") {
            return value;
        }
    }

    return undefined;
}

/**
 * Renames HTTP client spans from the bare method (e.g. "PUT") to "METHOD path" (e.g.
 * "PUT api/v1/Records"), so the Aspire dashboard's trace list shows which endpoint was called.
 * Attribute keys are probed in both stable and legacy semantic-convention spellings.
 */
class HttpSpanNameProcessor implements SpanProcessor {
    public onStart(span: Span): void {
        const url = getStringAttribute(span, "url.full", "http.url");
        if (url == null) {
            return;
        }

        const method = getStringAttribute(span, "http.request.method", "http.method");

        try {
            const path = new URL(url).pathname.replace(/^\//, "");
            span.updateName(`${method ?? span.name} ${path}`);
        } catch {
            // Leave the original span name when the URL cannot be parsed.
        }
    }

    public onEnd(): void { /* nothing to do */ }
    public async forceFlush(): Promise<void> { /* nothing to flush */ }
    public async shutdown(): Promise<void> { /* nothing to shut down */ }
}

/**
 * Marks a span as failed when the HTTP response status indicates an error. The browser
 * instrumentations do not do this on their own for 4xx responses, and without an Error status the
 * Aspire dashboard renders failed requests just like successful ones.
 */
function applyHttpStatusToSpan(span: ApiSpan, httpStatus: number): void {
    if (httpStatus >= 400 || httpStatus === 0) {
        span.setStatus({
            code: SpanStatusCode.ERROR,
            message: httpStatus === 0 ? "Network error" : `HTTP ${httpStatus}`
        });
    }
}

function attachSessionFallback(exporter: OTLPTraceExporter): OTLPTraceExporter {
    let isRuntimeDisabled = false;
    const originalExport = exporter.export.bind(exporter);

    exporter.export = (spans, resultCallback): void => {
        if (isRuntimeDisabled || isOpenTelemetryDisabledForSession()) {
            resultCallback({ code: 0 });
            return;
        }

        originalExport(spans, (result) => {
            if (!isSuccessfulExportResult(result)) {
                isRuntimeDisabled = true;
                disableOpenTelemetryForSession();
                void exporter.shutdown();
                resultCallback({ code: 0 });
                return;
            }

            resultCallback(result);
        });
    };

    return exporter;
}

/**
 * Initializes browser-side OpenTelemetry instrumentation once per page load.
 */
export function setupOpenTelemetry(): void {
    if (isOpenTelemetryInitialized) {
        return;
    }

    if (isOpenTelemetryDisabledForSession()) {
        isOpenTelemetryInitialized = true;
        return;
    }

    isOpenTelemetryInitialized = true;

    const serviceName = getNonEmptyValue(import.meta.env.VITE_OTEL_SERVICE_NAME) ?? DefaultServiceName;

    const exporter = attachSessionFallback(new OTLPTraceExporter({
        url: getTraceExporterUrl()
    }));

    const tracerProvider = new WebTracerProvider({
        resource: resourceFromAttributes({
            [ATTR_SERVICE_NAME]: serviceName,
            [ATTR_DEPLOYMENT_ENVIRONMENT_NAME]: import.meta.env.MODE
        }),
        spanProcessors: [
            new HttpSpanNameProcessor(),
            new BatchSpanProcessor(exporter, {
                scheduledDelayMillis: 1000,
                maxExportBatchSize: 20,
                maxQueueSize: 100
            })
        ]
    });

    tracerProvider.register();

    window.addEventListener("pagehide", () => {
        void tracerProvider.forceFlush();
    });

    registerInstrumentations({
        instrumentations: [
            new DocumentLoadInstrumentation(),
            new FetchInstrumentation({
                ignoreUrls: [/\/otlp\/v1\/traces$/],
                applyCustomAttributesOnSpan: (span, _request, result): void => {
                    if ("status" in result && typeof result.status === "number") {
                        applyHttpStatusToSpan(span, result.status);
                    }
                }
            }),
            new XMLHttpRequestInstrumentation({
                ignoreUrls: [/\/otlp\/v1\/traces$/],
                applyCustomAttributesOnSpan: (span, xhr): void => {
                    applyHttpStatusToSpan(span, xhr.status);
                }
            })
        ]
    });
}
