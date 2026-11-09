# syntax=docker/dockerfile:1

# ---------- Stage 1: build & publish ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first so the restore layer is cached until dependencies change.
COPY RaceDay.Web/RaceDay.Web.csproj RaceDay.Web/
RUN dotnet restore RaceDay.Web/RaceDay.Web.csproj

COPY RaceDay.Web/ RaceDay.Web/
RUN dotnet publish RaceDay.Web/RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: small runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Listen on 8080 inside the container; the API address is supplied at run time.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ApiBaseUrl=http://host.docker.internal:5000/ \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
EXPOSE 8080

# Run as the non-root "app" user that ships with the .NET 8 images.
USER app
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
