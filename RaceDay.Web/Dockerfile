# syntax=docker/dockerfile:1

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, on its own layer: it is only re-run when the project file changes.
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# Then copy the rest of the source and publish a trimmed-down Release build.
COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime stage (small image: runtime only, no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Listen on 8080 (non-privileged port) and run in Production, exactly like a real deployment.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0
EXPOSE 8080

COPY --from=build /app/publish ./

# Never run as root: the base image provides a built-in non-root user.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
