# SmartAttendanceSystem

ASP.NET Core 10 MVC/Razor application using Entity Framework Core, SQL Server,
ASP.NET Identity, QR attendance, and face enrollment/check-in.

Attendance QR codes are rendered by the application; session-token URLs are
not sent to a third-party QR-code service.

## Local development

The checked-in `appsettings.json` uses Windows integrated authentication and
the local `SmartAttendanceDB`. Keep that local connection string on the
development machine. Do not put Azure credentials in either appsettings file.
The Development environment retains its local demo administrator fallback;
deployed environments require the separate `AdminBootstrap__...` settings.

In Development, face images are stored under
`SmartAttendanceSystem/wwwroot/uploads/faces/`. The directory is ignored by
Git. Deployed environments use a private Azure Blob container instead.

## Azure production deployment

The repository includes `.github/workflows/azure-webapp.yml`. A push to `main`
builds the existing ASP.NET Core project and deploys it to Azure App Service.
No database migration is run automatically at application startup.

### 1. Create Azure SQL Database and preserve existing data

1. In the Azure Portal, create a logical SQL server. Keep it in the same region
   as the App Service.
2. If the local `SmartAttendanceDB` contains data you need, back it up before
   changing anything. In SSMS, right-click the database and select
   **Tasks > Export Data-tier Application** to create a `.bacpac`. Upload that
   file to a private Azure Blob container (for example, `database-import`)
   using Azure Storage Explorer. Create the storage account/container first if
   needed; the account can also be used for face photos below. In the Azure SQL
   server's **Import database** action, provide the BACPAC blob URI and a
   short-lived read-only SAS token, then import it as `SmartAttendanceDB`.
   Revoke the SAS after the import.
   Alternatively, use Azure Database Migration Service for databases that
   cannot be exported to BACPAC. Never add a `.bacpac` or `.bak` file to Git.
3. If you do not need the local data, create a new Azure **SQL Database** named
   `SmartAttendanceDB` on that server instead.
4. In the Azure SQL server's **Networking** page, allow connections from your
   current IP for database setup. Avoid enabling broad public access if
   specific IP rules meet your needs.
5. Create a dedicated SQL database user for the application and grant only
   `db_datareader` and `db_datawriter`. Use the SQL server administrator only
   for database setup and migration work, not as the application's runtime
   account. Run this while connected to `SmartAttendanceDB`, replacing both
   placeholders locally:

   ```sql
   CREATE USER [attendance_app] WITH PASSWORD = '<strong-password>';
   ALTER ROLE db_datareader ADD MEMBER [attendance_app];
   ALTER ROLE db_datawriter ADD MEMBER [attendance_app];
   ```

### 2. Apply EF Core migrations safely

An idempotent script for all migrations currently in the repository is checked
in at `SmartAttendanceSystem/Migrations/AzureSqlMigrations.sql`. Review that
script, then connect SSMS to the Azure SQL database and execute it. For a
BACPAC import, the EF migration history is carried with the database; the
script applies only migrations that are missing. For a new empty database, it
creates the schema. Do not enable automatic migrations in application startup.
Before applying it to an imported database, verify that
`__EFMigrationsHistory` exists and reflects the schema already present. If the
schema was changed outside EF and migration history is missing or incomplete,
stop and reconcile/baseline it before running the script; do not blindly apply
an initial-create script to tables that already exist.

```powershell
dotnet tool update --global dotnet-ef --version 10.0.10
Push-Location .\SmartAttendanceSystem
dotnet ef migrations script --idempotent --output .\Migrations\AzureSqlMigrations.sql
Pop-Location
```

Regenerate the script using the command above whenever new migrations are
added, and review the SQL before applying it to production.

### 3. Create private storage for face photos

1. In the Storage account, create a Blob container named `face-images`. Set
   the container's anonymous/public access level to **Private**.
2. App Service uses the account endpoint and container name in settings below.
   It does not need a storage account key or a public container.
3. Existing photo files on the development machine are not in SQL and are not
   deployed with the app. If you need those profile photos in production, use
   Azure Storage Explorer to upload the contents of
   `SmartAttendanceSystem/wwwroot/uploads/faces/` to the private container
   under the `uploads/faces/` virtual folder. Preserve the original filenames.
   Face embeddings remain in SQL Server and do not require these photos for
   recognition.

### 4. Create Azure App Service

1. Create an **App Service** using **Publish: Code**, **Runtime stack: .NET 10**,
   and Linux. Select a region matching the database and a plan that supports
   your expected traffic.
2. In the App Service **Identity** page, turn on **System assigned** and save.
   In the Storage account's **Access control (IAM)**, assign that identity the
   **Storage Blob Data Contributor** role. Allow a few minutes for the role
   assignment to propagate.
3. Add the App Service outbound IP addresses (shown under **Properties**) to
   the Azure SQL server's firewall rules.
4. In **Configuration > Application settings**, add:

   | Name | Value |
   | --- | --- |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |
   | `ConnectionStrings__DefaultConnection` | Azure SQL connection string (example below) |
   | `AdminBootstrap__Username` | New production administrator username |
   | `AdminBootstrap__Email` | Administrator email |
   | `AdminBootstrap__Password` | At least 12 characters, including uppercase, digit, and symbol |
   | `FaceStorage__BlobServiceUri` | `https://<storage-account>.blob.core.windows.net` |
   | `FaceStorage__ContainerName` | `face-images` |

   An Azure SQL connection string has this shape; replace every placeholder in
   the Portal and keep the real password out of source control:

   ```text
   Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=SmartAttendanceDB;Persist Security Info=False;User ID=<app-user>;Password=<strong-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

   Save the settings and restart the App Service. The application requires the
   database and Blob settings in deployed environments. Its configured admin
   account is created if missing; if that username already exists, its
   password is aligned with `AdminBootstrap__Password` and the Admin role is
   ensured.

### 5. Connect GitHub and deploy `main`

1. In GitHub, open **Settings > Secrets and variables > Actions > Variables**.
   Add `AZURE_WEBAPP_NAME` with the exact App Service name.
2. In the Azure Portal, open the App Service **Overview > Get publish profile**
   and download the profile. Treat the downloaded XML as a password; do not
   commit it or paste it into chat.
3. In GitHub **Settings > Secrets and variables > Actions > Secrets**, add
   `AZURE_WEBAPP_PUBLISH_PROFILE` and paste the entire publish-profile XML.
4. In GitHub, open **Actions**, enable workflows if asked, then run
   **Deploy SmartAttendanceSystem to Azure App Service** using **Run workflow**
   on `main`. Later pushes to `main` deploy automatically.

### 6. Get the public URL and test

The public address is:

```text
https://<app-name>.azurewebsites.net
```

From the Azure Portal, open the App Service **Overview** and click **Default
domain**. Confirm **HTTPS Only** is enabled under **TLS/SSL settings**.

Test the production administrator login, create or verify teacher and student
accounts, then test dashboard access, QR session creation/check-in, face
enrollment, face check-in, and role restrictions. Test camera features from a
phone or computer browser and grant camera permission; camera access requires
HTTPS. Verify SQL firewall access and Blob permissions if profile images or
enrollment fail.

### 7. Troubleshoot deployment

- GitHub build/deploy: repository **Actions > latest workflow run**.
- Application startup/runtime logs: Azure Portal **App Service > Monitoring >
  Log stream**. Enable **App Service logs > Application logging (Filesystem)**
  first if the stream is empty.
- SQL connection errors: check the connection string setting, SQL firewall
  rules, and the App Service outbound IP addresses.
- Face photo authorization/storage errors: check the Blob container is private,
  the system identity has **Storage Blob Data Contributor**, and the two
  `FaceStorage__...` settings match the account and container.

Do not share publish profiles, database passwords, storage keys, or user
session cookies. `.gitignore` excludes common local cookie, face-photo, and
publish-profile files.
