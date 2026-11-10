# Build context must be the solution root:  docker build -f ProductAPI/Dockerfile -t ProductAPI .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Shared/ECommerce.Shared.csproj ECommerce.Shared/
COPY ProductAPI/ProductAPI.csproj ProductAPI/
RUN dotnet restore ProductAPI/ProductAPI.csproj
COPY ECommerce.Shared/ ECommerce.Shared/
COPY ProductAPI/ ProductAPI/
RUN dotnet publish ProductAPI/ProductAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "ProductAPI.dll"]
