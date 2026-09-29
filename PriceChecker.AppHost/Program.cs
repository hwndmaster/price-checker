using Genius.PriceChecker.AppHost;

// The port the Vite dev server runs on locally. Named in one more place that has to agree with it:
// `server.port` in vite.config.ts, for when the dev server is started on its own.
const int WebDevServerPort = 5081;

var builder = DistributedApplication.CreateBuilder(args);

// ── Modes ────────────────────────────────────────────────────────────────────────────────────
// (unset)    Local development: the app host builds and supervises the projects from source.
// deployed   Containerized deployment: DCP owns the api and web containers, so the dashboard's
//            Resources page can start, stop, restart and stream logs for them.
// external   Containerized deployment where compose owns the containers and the app host only
//            observes them. The fallback for when DCP cannot drive the container runtime.
// dashboard  Telemetry sink only — no resource model, so the Resources page stays empty.
switch (builder.Configuration["AppHost:Mode"]?.ToLowerInvariant())
{
    case "dashboard":
        break;

    case "deployed":
        ConfigureDeployed(builder);
        break;

    case "external":
        ConfigureExternal(builder);
        break;

    default:
        ConfigureLocalDevelopment(builder);
        break;
}

await builder.Build().RunAsync().ConfigureAwait(false);

static void ConfigureLocalDevelopment(IDistributedApplicationBuilder builder)
{
    var api = builder.AddProject<Projects.WebApi>("webapi")
        .WithHttpHealthCheck("/health");

    var web = builder.AddViteApp("web", "../PriceChecker.Web", "start:aspire")
        .WithPnpm()
        .WithReference(api)
        // AddViteApp leaves the endpoint's ports unset, so Aspire allocates a fresh pair on every run:
        // one for its proxy and one it passes to vite as --port. The dev server then moved every time,
        // and two different ports served the SPA at once.
        //
        // Unproxied on purpose. Proxying would still hand vite a random port of its own, and the point
        // here is that the dev server sits where vite.config.ts and .env already say it does - the same
        // address as a plain `pnpm start`, with vite's HMR socket reaching it without a hop in between.
        .WithEndpoint("http", endpoint =>
        {
            endpoint.Port = WebDevServerPort;
            endpoint.TargetPort = WebDevServerPort;
            endpoint.IsProxied = false;
        })
        .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
        .WithExternalHttpEndpoints()
        .WaitFor(api);

    // The browser's OTLP exporter always posts to <origin>/otlp. In the published image nginx carries
    // that on to the dashboard; the dev server has to make the same hop, so it is told where to.
    //
    // Passed explicitly rather than derived from the OTEL_* variables Aspire injects into the node
    // process: those address the gRPC endpoint, and a browser exporter can only speak OTLP/HTTP.
    //
    // Left unset — an app host started without this launch profile — vite.config.ts configures no proxy
    // and the SPA leaves telemetry off, rather than posting into a dev server that has no such route.
    var otlpHttpEndpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"];
    if (!string.IsNullOrWhiteSpace(otlpHttpEndpoint))
    {
        web.WithEnvironment("VITE_OTLP_UPSTREAM", otlpHttpEndpoint);
    }

    api.WithEnvironment("Cors__Origins__0", web.GetEndpoint("http"));
}

/// <summary>
/// DCP creates and supervises the published images as sibling containers on the host's Docker daemon
/// (its socket is bind-mounted into this container). Compose only starts this app host; everything else
/// is owned here, which is what makes the Resources page controllable rather than empty.
/// </summary>
static void ConfigureDeployed(IDistributedApplicationBuilder builder)
{
    var settings = DeploymentSettings.From(builder.Configuration);

    // No "--network" runtime argument anywhere below. DCP reconciles the networks of the containers it
    // owns, and an externally supplied network makes it try to detach the container from a network it
    // has no record of, which fails the start outright with:
    //   ContainerReconciler  Could not detach network from the container ... network ... not found
    // DCP puts everything it owns on one network of its own, so pricechecker-web already resolves
    // pricechecker-api by name there. Only the hop back to this app host leaves that network, and that
    // goes over the host gateway instead.
    var api = builder.AddContainer("webapi", $"{settings.Registry}/pricechecker-api", settings.ImageTag)
        // The web image's nginx.conf proxies to pricechecker-api by name, so the container name is part
        // of the contract rather than a cosmetic choice.
        .WithContainerName("pricechecker-api")
        .WithImagePullPolicy(settings.ImagePullPolicy)
        // Persistent: restarting this app host must not take the application down with it.
        .WithLifetime(ContainerLifetime.Persistent)
        // No explicit host port, and the reachable publish is added as a runtime argument below.
        // DCP always publishes its own endpoints on 127.0.0.1 (DcpPublisher:BindAddress does not change
        // that), which on a server would leave the app reachable only from the machine itself.
        .WithHttpEndpoint(targetPort: DeploymentSettings.ApiContainerPort, isProxied: false)
        .WithContainerRuntimeArgs("-p", $"{settings.BindAddress}:{settings.ApiPort}:{DeploymentSettings.ApiContainerPort}")
        // The endpoint's own URL is DCP's 127.0.0.1 publish, which only works from this machine, so the
        // dashboard is given the reachable publish above instead. Display only: nothing connects by it.
        .WithUrlForEndpoint("http", url => url.Url = settings.PublicUrl(settings.ApiPort))
        // Deliberately runtime arguments rather than WithBindMount. WithBindMount normalises the path
        // with the APP HOST's OS conventions, but this app host runs in a Linux container while the
        // daemon resolving the mount is the Windows host's. A "C:/..." path is not absolute to Linux, so
        // it got prefixed with the app host's working directory and the container failed to create:
        //   bind source path does not exist: /src/PriceChecker.AppHost/C:/...
        // Passed as -v, the string reaches the daemon untouched.
        .WithContainerRuntimeArgs("-v", $"{settings.DataPath}:/app/Data")
        .WithContainerRuntimeArgs("-v", $"{settings.LogsPath}:/app/Logs")
        // Docker Desktop resolves host.docker.internal on its own; a plain Linux daemon does not, hence
        // the explicit host-gateway mapping. Harmless where it is already provided.
        .WithContainerRuntimeArgs("--add-host", "host.docker.internal:host-gateway")
        // Docker Desktop groups containers by these labels. Set explicitly rather than left to chance:
        // `docker compose build` bakes the BUILDING project's name into the image (the repo directory,
        // "price-checker"), containers inherit image labels, and the result was the app host sitting in
        // one stack while the containers it owns sat in another. Compose does not claim containers just
        // because they carry its project label, so the persistent lifetime is unaffected.
        .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={settings.ComposeProject}")
        .WithContainerRuntimeArgs("--label", "com.docker.compose.service=pricechecker-api")
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", settings.OtlpGrpcEndpoint)
        .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
        .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE", "true")
        // Aspire injects this for project resources but not for plain containers, and without it every
        // log line and span arrives in the dashboard under "unknown_service:dotnet" — indistinguishable
        // from anything else reporting in.
        .WithEnvironment("OTEL_SERVICE_NAME", "pricechecker-api");

    builder.AddContainer("web", $"{settings.Registry}/pricechecker-web", settings.ImageTag)
        .WithContainerName("pricechecker-web")
        .WithImagePullPolicy(settings.ImagePullPolicy)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHttpEndpoint(targetPort: DeploymentSettings.WebContainerPort, isProxied: false)
        .WithContainerRuntimeArgs("-p", $"{settings.BindAddress}:{settings.WebPort}:{DeploymentSettings.WebContainerPort}")
        .WithUrlForEndpoint("http", url => url.Url = settings.PublicUrl(settings.WebPort))
        .WithExternalHttpEndpoints()
        .WithContainerRuntimeArgs("--add-host", "host.docker.internal:host-gateway")
        .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={settings.ComposeProject}")
        .WithContainerRuntimeArgs("--label", "com.docker.compose.service=pricechecker-web")
        // nginx in this image renders its config from these at start-up. Its defaults assume every
        // container shares one network, which is true when compose owns them but not here.
        .WithEnvironment("PRICECHECKER_API_UPSTREAM", settings.WebApiUpstream)
        .WithEnvironment("PRICECHECKER_OTLP_UPSTREAM", settings.WebOtlpUpstream)
        .WaitFor(api);
}

/// <summary>
/// Compose owns the containers; the app host only reports them. Needs no container runtime access, so
/// it works without the Docker socket — at the cost of a read-only Resources page.
/// </summary>
static void ConfigureExternal(IDistributedApplicationBuilder builder)
{
    builder.AddExternalService("webapi", $"http://pricechecker-api:{DeploymentSettings.ApiContainerPort}")
        .WithHttpHealthCheck("/health");

    builder.AddExternalService("web", $"http://pricechecker-web:{DeploymentSettings.WebContainerPort}")
        .WithHttpHealthCheck("/");
}
