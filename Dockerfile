# Multi-stage build for Sonrisa Notifier Host
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything and restore
COPY . .
RUN dotnet restore "sonrisa_notifier.sln"

# Publish the Host project
RUN dotnet publish "src/Sonrisa.Notifier.Host/Sonrisa.Notifier.Host.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published host
COPY --from=build /app/publish .

# Copy Admin static files and Infrastructure data so the Host can find them using its relative paths
COPY --from=build /src/src/Sonrisa.Notifier.Admin/wwwroot /Sonrisa.Notifier.Admin/wwwroot
COPY --from=build /src/src/Sonrisa.Notifier.Infrastructure/Data /Sonrisa.Notifier.Infrastructure/Data

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Sonrisa.Notifier.Host.dll"]
