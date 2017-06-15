FROM microsoft/dotnet:1.1.1-runtime-nanoserver

COPY ./out /app
WORKDIR /app

EXPOSE 5003

ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://*:5003

ENTRYPOINT ["dotnet", "Rsk.Samples.IdentityServer4.AdminUiIntegration.dll"]