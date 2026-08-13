# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything from the repo root (needed for project references)
COPY . .

# Configure GitHub Packages credentials through a build secret named
# atom_pkg_access_token passed from docker-compose.

# Restore tools at repo root so Directory.Build.props imports resolve.
WORKDIR /src
RUN --mount=type=secret,id=atom_pkg_access_token \
	--mount=type=cache,id=pricechecker-apphost-nuget-packages,target=/root/.nuget/packages \
	--mount=type=cache,id=pricechecker-apphost-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
	sh /src/scripts/docker-dotnet-restore.sh tool /src

WORKDIR /src/PriceChecker.AppHost
RUN --mount=type=secret,id=atom_pkg_access_token \
	--mount=type=cache,id=pricechecker-apphost-nuget-packages,target=/root/.nuget/packages \
	--mount=type=cache,id=pricechecker-apphost-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
	sh /src/scripts/docker-dotnet-restore.sh project /src/PriceChecker.AppHost
RUN --mount=type=cache,id=pricechecker-apphost-nuget-packages,target=/root/.nuget/packages \
	--mount=type=cache,id=pricechecker-apphost-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
	dotnet build /src/PriceChecker.AppHost/PriceChecker.AppHost.csproj -c Release --no-restore /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false
RUN --mount=type=cache,id=pricechecker-apphost-nuget-packages,target=/root/.nuget/packages \
	sh -eu -c 'mkdir -p /aspire-runtime-packages; \
	for package in aspire.hosting.orchestration.linux-x64 aspire.dashboard.sdk.linux-x64; do \
		if [ ! -d "/root/.nuget/packages/$package" ]; then \
			echo "Required Aspire package not found in NuGet cache: $package" >&2; \
			exit 1; \
		fi; \
		cp -a "/root/.nuget/packages/$package" /aspire-runtime-packages/; \
	done'

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /src/PriceChecker.AppHost/bin/Release/net10.0 .
COPY --from=build /aspire-runtime-packages /root/.nuget/packages

ENV ASPNETCORE_URLS=http://+:15180
ENV ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
ENV ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://0.0.0.0:21180
ENV ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL=http://0.0.0.0:21181
ENV AppHost__Mode=dashboard

EXPOSE 15180
EXPOSE 21180
EXPOSE 21181

ENTRYPOINT ["dotnet", "Genius.PriceChecker.AppHost.dll"]
