# Integration Tests

Run the TinyShop integration tests with:

```bash
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
```

If SQL Server is provided externally (for example via the Docker Compose stack or a local SQL Server instance), set the database connection string by substituting the password from the current environment.

```bash
export ConnectionStrings__TinyShopDB="Server=localhost,1433;Database=TinyShopDB;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=False;"
```

Then run:

```bash
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
```
