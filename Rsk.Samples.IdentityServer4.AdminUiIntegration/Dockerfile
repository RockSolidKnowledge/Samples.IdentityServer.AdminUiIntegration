FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

COPY ./out /app
COPY ./wait-for-it.sh /app
COPY ./MySqlStart.sh /app
WORKDIR /app

RUN apt-get update
RUN apt-get install -y dos2unix
RUN dos2unix /app/wait-for-it.sh  && dos2unix /app/MySqlStart.sh && apt-get --purge remove -y dos2unix && rm -rf /var/lib/apt/lists/*
RUN chmod +x /app/wait-for-it.sh
RUN chmod +x /app/MySqlStart.sh
RUN mkdir /app/https
EXPOSE 5003

ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_HTTPS_PORT=5003
ENV ASPNETCORE_URLS https://*:5003

#Not Suitable For Production!
RUN openssl req -x509 -newkey rsa:4096 -keyout /app/https/localhost.key -out /app/https/localhost.crt -passout pass:"Password123!" -days 3650 -sha256 -subj '/CN=localhost' -addext "subjectAltName=DNS:localhost,DNS:ids"
RUN cp -R /app/https/* /usr/local/share/ca-certificates
RUN openssl pkcs12 -export -passin pass:"Password123!" -passout pass:"Password123!" -out /app/https/aspnetapp.pfx -inkey /app/https/localhost.key -in /app/https/localhost.crt
RUN update-ca-certificates
RUN chmod 777 /usr/local/share/ca-certificates/localhost.crt

ENV ASPNETCORE_Kestrel__Certificates__Default__Password="Password123!"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path="/app/https/aspnetapp.pfx"

ENTRYPOINT ["dotnet", "Rsk.Samples.IdentityServer4.AdminUiIntegration.dll"]