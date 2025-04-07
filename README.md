# dotnet-odoo-example
.Net JSON-RPC example for Odoo Integration

This app was built using [.Net SDK v8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) 

1. Run: `dotnet add package DotNetEnv` to Install DotNetEnv dependency
2. Create a .env file
3. Add the following values:
   ```
    ODOO_URL=https://<dev-database>.dev.odoo.com/
    ODOO_DB=<dev-database>
    ODOO_USER=<username>
    ODOO_PASSWORD=<password>
   ```
4. Close and re-open your IDE
5. For building run: `dotnet build`
6. For testing run: `dotnet run`
