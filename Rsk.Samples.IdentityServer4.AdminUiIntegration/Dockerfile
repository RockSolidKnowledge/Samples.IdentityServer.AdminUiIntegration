FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine

COPY ./bin/Release/netcoreapp3.1/publish/ /app
WORKDIR /app

EXPOSE 5003

RUN apk add --no-cache icu-libs

ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_HTTP_PORT=5003
ENV ASPNETCORE_URLS http://*:5003
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "Rsk.Samples.IdentityServer4.AdminUiIntegration.dll"]
