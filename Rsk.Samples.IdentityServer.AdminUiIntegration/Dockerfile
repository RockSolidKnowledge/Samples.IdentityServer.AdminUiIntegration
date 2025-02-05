# Stage 1 - Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH

WORKDIR /app

# packages installed
RUN apk update && \
    apk upgrade

# Copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

# Copy everything else and build
COPY . .
RUN dotnet publish -a $TARGETARCH -o out

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORT=5003
ENV ASPNETCORE_URLS=http://*:5003
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 5003

WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Rsk.Samples.IdentityServer.AdminUiIntegration.dll"]