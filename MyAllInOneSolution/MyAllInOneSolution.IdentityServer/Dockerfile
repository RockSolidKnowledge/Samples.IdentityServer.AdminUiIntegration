# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o out

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS runtime

ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_HTTP_PORT=5003
ENV ASPNETCORE_URLS http://*:5003
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 5003

RUN apk add --no-cache icu-libs

WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "MyAllInOneSolution.IdentityServer.dll"]