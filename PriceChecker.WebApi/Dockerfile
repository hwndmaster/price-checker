# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything from the repo root (needed for project references)
COPY . .

# Configure GitHub Packages credentials through a build secret named
# atom_pkg_access_token passed from docker-compose.

WORKDIR /src/PriceChecker.WebApi
RUN --mount=type=secret,id=atom_pkg_access_token \
	--mount=type=cache,id=pricechecker-api-nuget-packages,target=/root/.nuget/packages \
	--mount=type=cache,id=pricechecker-api-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
	sh /src/scripts/docker-dotnet-restore.sh tool /src/PriceChecker.WebApi
RUN --mount=type=secret,id=atom_pkg_access_token \
	--mount=type=cache,id=pricechecker-api-nuget-packages,target=/root/.nuget/packages \
	--mount=type=cache,id=pricechecker-api-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
	sh /src/scripts/docker-dotnet-restore.sh project /src/PriceChecker.WebApi linux-x64
RUN --mount=type=cache,id=pricechecker-api-nuget-packages,target=/root/.nuget/packages \
	--mount=type=cache,id=pricechecker-api-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
	dotnet publish -c Release -r linux-x64 --self-contained false /p:UseAppHost=false \
		/p:DebugType=None /p:DebugSymbols=false --no-restore -o /app/publish

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8031
EXPOSE 8031
ENTRYPOINT ["dotnet", "PriceChecker.WebApi.dll"]
