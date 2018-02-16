FROM microsoft/dotnet:2.0-runtime-jessie

COPY ./out /app
COPY ./wait-for-it.sh /app
COPY ./MySqlStart.sh /app
WORKDIR /app

RUN apt-get update
RUN apt-get install -y dos2unix
RUN dos2unix /app/wait-for-it.sh  && dos2unix /app/MySqlStart.sh && apt-get --purge remove -y dos2unix && rm -rf /var/lib/apt/lists/*
RUN chmod +x /app/wait-for-it.sh
RUN chmod +x /app/MySqlStart.sh

EXPOSE 5003

ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://*:5003

ENTRYPOINT ["dotnet", "Rsk.Samples.IdentityServer4.AdminUiIntegration.dll"]