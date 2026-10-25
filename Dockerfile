# Headless dedicated server (`--server`). The Raylib window is never opened in this mode,
# so the image only needs the ASP.NET runtime.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Isaac.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
# game.log is written to the working directory, so run from a folder the non-root user owns.
RUN mkdir /data && chown app /data
WORKDIR /data
USER app
# The base image sets HTTP_PORTS=8080; clear it, the server picks its own port.
ENV ASPNETCORE_HTTP_PORTS=
EXPOSE 7777
ENTRYPOINT ["dotnet", "/app/Isaac.dll", "--server"]
