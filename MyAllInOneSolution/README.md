# AdminUI and IdentityServer All In One

Welcome to AdminUI All In On. This solution combines AdminUI with IdentityServer.


## Getting Started: Minimal Configuration

To get up and running on your local machine with this integrated solution, ensure you have these settings in place:

- __AdminUI license key__: Locate the "LicenseKey" field within the appsettings.json file in the AdminUI project and add your license key. You can get a free one [here](https://www.identityserver.com/products/adminui).
- __Database provider__: You must add this in 2 location, these are the appsettings files in the AdminUI project and in the IdentityServer project. Specify your desired database provider using the "DbProvider" field. Options are: SqlServer, MySql or PostgreSql.
- __Database connection string__: Same as before, you must add this in each appsetting file, in AdminUI and IdentityServer projects. Provide the connection strings for both Identity and IdentityServer databases using the "IdentityConnectionString" and "IdentityServerConnectionString" fields respectively. It can be the same database for both, therefore same connection string. Here are some examples of connection strings:

For SqlServer: 
“Server=localhost;User Id=myuser;Password=Password123!;Database=IdentityExpress;” 

For MySql: 
“Server=localhost; Port=3306; Uid=myuser; Pwd=Password123!; Database=identityexpress;” 

For PostgreSql: 
“Server=localhost; Port=5432; Uid=myuser;Pwd=Password123!;Database=IdentityExpress;” 

- __URLs (Optional)__: The default URLs where AdminUI and IdentityServer will listen are:

- AdminUI: https://localhost:5001
- IdentityServer: https://localhost:5003

If you want to use different URLs, you have to change them in the launchSettings files of each project (AdminUI and IdentityServer) and, in the appsettings file of the AdminUI project. In that appsettings file you will find AdminUI URL as "UiUrl" and IdentityServer URL as "AuthorityUrl"

__Warning:__ 
Please be aware that this solution contains also other many settings preconfigured to get your solution ready to run only on your local machine. This means that certain settings, such as client secret or IdentityServer license, must be carefully reviewed and modified for production use. Please, find more information below.

## Initial Setup Instructions
For the initial setup, please follow these steps to ensure a successful deployment:
1. If you haven't configured the minimal settings during the solution creation, please refer to the previous section and set up the necessary configuration.
2. Start by running only the AdminUI project and wait until migrations have finished.
3. Once the AdminUI migrations have finished, proceed to run the IdentityServer project.

By running applications in this order, you prevent IdentityServer encountering an empty database. After this initial setup, feel free to run both projects at the same time.

## AdminUI project

The AdminUI project leverages the power of the [AdminUI NuGet package](https://www.nuget.org/packages/Rsk.AdminUI). It comes pre-configured to streamline your setup process. However, if you wish to further customize your solution, refer to the [documentation](https://www.identityserver.com/documentation/adminui/).

## IdentityServer project

This project is the IdentityServer sample created by [Rock Solid Knowledge](https://www.identityserver.com) to exemplify the configuration needed to integrate it with AdminUI. You can find the sample in this [repository](https://github.com/RockSolidKnowledge/Samples.IdentityServer.AdminUiIntegration).

This sample relies on the ASP.NET Identity schema from the [AdminUI product](https://www.identityserver.com/products/adminui) offered by [Rock Solid Knowledge](https://www.identityserver.com). You can access the AdminUI ASP.NET Identity schema via [NuGet](https://www.nuget.org/packages/IdentityExpress.Identity/):

`Install-Package IdentityExpress.Identity`

---

__Warning:__ 
Please be aware that this package employs a pre-generated embedded certificate and is intended for non-production use.
