# Build context must be the solution root:  docker build -f gateway/Dockerfile -t gateway .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Shared/ECommerce.Shared.csproj ECommerce.Shared/
COPY gateway/Gateway.csproj gateway/
RUN dotnet restore gateway/Gateway.csproj
COPY ECommerce.Shared/ ECommerce.Shared/
COPY gateway/ gateway/
RUN dotnet publish gateway/Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
