# Builds the authoritative game server as a self-contained image.
# Only the server and its dependencies are needed - the WinForms client is Windows-only
# and never runs here.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Unhallowed.Contracts/ src/Unhallowed.Contracts/
COPY src/Unhallowed.Core/ src/Unhallowed.Core/
COPY src/Unhallowed.Server/ src/Unhallowed.Server/

RUN dotnet restore src/Unhallowed.Server/Unhallowed.Server.csproj
RUN dotnet publish src/Unhallowed.Server/Unhallowed.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Kestrel is configured to listen on 0.0.0.0:5080 in appsettings.json.
EXPOSE 5080
ENTRYPOINT ["dotnet", "Unhallowed.Server.dll"]
