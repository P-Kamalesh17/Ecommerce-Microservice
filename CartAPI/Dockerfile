# Build context must be the solution root:  docker build -f CartAPI/Dockerfile -t CartAPI .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Shared/ECommerce.Shared.csproj ECommerce.Shared/
COPY CartAPI/CartAPI.csproj CartAPI/
RUN dotnet restore CartAPI/CartAPI.csproj
COPY ECommerce.Shared/ ECommerce.Shared/
COPY CartAPI/ CartAPI/
RUN dotnet publish CartAPI/CartAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "CartAPI.dll"]
