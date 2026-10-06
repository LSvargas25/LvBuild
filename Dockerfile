# syntax=docker/dockerfile:1

# ---- build ---------------------------------------------------------------------------------
# The solution file is .slnx, which the .NET 8 SDK cannot read, so LvApi.csproj is restored and
# published directly (its project references bring Application, Domain and Infrastructure).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, from the project files only, so this layer is cached until a dependency changes.
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY LvApi/LvApi.csproj LvApi/
COPY LvApplication/LvApplication.csproj LvApplication/
COPY LvDomain/LvDomain.csproj LvDomain/
COPY LvInfrastructure/LvInfrastructure.csproj LvInfrastructure/
RUN dotnet restore LvApi/LvApi.csproj

COPY LvApi/ LvApi/
COPY LvApplication/ LvApplication/
COPY LvDomain/ LvDomain/
COPY LvInfrastructure/ LvInfrastructure/
RUN dotnet publish LvApi/LvApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ---- runtime -------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# QuestPDF (offer PDFs) renders text through fontconfig and needs at least one font installed.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT and the app listens on it (see HostingExtensions.UsePortFromEnvironment);
# without PORT the image default below applies.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Non-root: the aspnet image ships an unprivileged "app" user (UID in $APP_UID).
USER $APP_UID

ENTRYPOINT ["dotnet", "LvApi.dll"]
