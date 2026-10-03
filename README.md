# SmartAttendanceSystem

## Deploy to Railway

Railway builds the Docker image from the repository root using `Dockerfile`. Add these service variables in Railway before deploying:

- `ConnectionStrings__DefaultConnection`: a SQL Server connection string for a SQL Server Railway can reach.
- `AdminBootstrap__Username`: username for the production administrator.
- `AdminBootstrap__Email`: email for the production administrator.
- `AdminBootstrap__Password`: a new, strong password for the production administrator.

Keep these values in Railway's Variables settings; do not commit credentials to this repository.

The development connection string points to `localhost` and uses Windows integrated authentication. It cannot be used by Railway. Move or restore `SmartAttendanceDB` to a remotely reachable SQL Server, then use that server's connection string. The application does not automatically migrate the database at startup, so apply the existing EF Core migrations to the remote database before deploying.

On first startup, the configured administrator is created if it does not exist. If the same username already exists in the database, its password is changed to `AdminBootstrap__Password` and its Admin role is ensured. The development-only fallback credentials are not displayed by the public login page.
