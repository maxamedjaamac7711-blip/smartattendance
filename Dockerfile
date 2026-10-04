FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SmartAttendanceSystem/SmartAttendanceSystem.csproj", "SmartAttendanceSystem/"]
RUN dotnet restore "SmartAttendanceSystem/SmartAttendanceSystem.csproj"

COPY . .
WORKDIR /src/SmartAttendanceSystem
RUN dotnet publish "SmartAttendanceSystem.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet SmartAttendanceSystem.dll --urls http://0.0.0.0:${PORT:-8080}"]
