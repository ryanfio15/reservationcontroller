# One image serves the Blazor Web App and /api/v1, and the same image runs the migration job and
# the optional scheduled job with a different command (B: System architecture).
#
# Build context is the repository root, so Directory.Build.props, Directory.Packages.props and
# global.json are all visible to the restore.

# --- restore ----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS restore
WORKDIR /src

# Copied first and alone, so a change to a .cs file does not invalidate the restore layer.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY ReservationController.slnx ./
COPY src/Domain/Domain.csproj                                   src/Domain/
COPY src/Infrastructure/Infrastructure.csproj                   src/Infrastructure/
COPY src/Jobs/Jobs.csproj                                       src/Jobs/
COPY src/Web/Web.csproj                                         src/Web/
COPY src/Web.Client/Web.Client.csproj                           src/Web.Client/
COPY src/Shared/App.ApiClient/App.ApiClient.csproj               src/Shared/App.ApiClient/
COPY src/Shared/App.Auth/App.Auth.csproj                         src/Shared/App.Auth/
COPY src/Shared/App.Core/App.Core.csproj                         src/Shared/App.Core/
COPY src/Shared/App.UI/App.UI.csproj                             src/Shared/App.UI/
COPY src/Features/Features.Admin/Features.Admin.csproj           src/Features/Features.Admin/
COPY src/Features/Features.Diner/Features.Diner.csproj           src/Features/Features.Diner/
COPY src/Features/Features.Host/Features.Host.csproj             src/Features/Features.Host/
COPY tests/Unit/Unit.csproj                                      tests/Unit/

RUN dotnet restore src/Web/Web.csproj && dotnet restore src/Jobs/Jobs.csproj

# --- publish ----------------------------------------------------------------------------------
FROM restore AS publish
COPY . .
RUN dotnet publish src/Web/Web.csproj \
        --no-restore \
        --configuration Release \
        -p:ContinuousIntegrationBuild=true \
        --output /app

# --- runtime ----------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

# tzdata: reservation times are shown in each restaurant's zone (B: Time zones) and the aspnet
# image does not carry the time-zone database.
# curl: the compose health check only; the deployed Container App probes /healthz itself.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0

EXPOSE 8080

# The aspnet image already defines the non-root `app` user.
USER app

COPY --from=publish /app .

# Default command serves the app. The scheduled job runs the same image with
# ["dotnet", "jobs/Jobs.dll"]; phase 9's migration job overrides the command the same way.
ENTRYPOINT ["dotnet", "Web.dll"]
