# Stage 1: Build and publish the API
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy central build and package configuration
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY backend/SchoolAdmission.sln backend/
COPY backend/src/SchoolAdmission.Api/SchoolAdmission.Api.csproj backend/src/SchoolAdmission.Api/
COPY backend/src/SchoolAdmission.Api/packages.lock.json backend/src/SchoolAdmission.Api/
COPY backend/tests/SchoolAdmission.Api.IntegrationTests/SchoolAdmission.Api.IntegrationTests.csproj backend/tests/SchoolAdmission.Api.IntegrationTests/
COPY backend/tests/SchoolAdmission.Api.IntegrationTests/packages.lock.json backend/tests/SchoolAdmission.Api.IntegrationTests/

# Restore dependencies using locked mode
RUN dotnet restore backend/SchoolAdmission.sln --locked-mode

# Copy backend source code
COPY backend/src/ backend/src/

# Publish the API project in Release mode
WORKDIR /src/backend/src/SchoolAdmission.Api
RUN dotnet publish SchoolAdmission.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Stage 2: ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Run as non-root user
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
