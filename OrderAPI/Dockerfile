# Build context must be the solution root:  docker build -f OrderAPI/Dockerfile -t OrderAPI .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Shared/ECommerce.Shared.csproj ECommerce.Shared/
COPY OrderAPI/OrderAPI.csproj OrderAPI/
RUN dotnet restore OrderAPI/OrderAPI.csproj
COPY ECommerce.Shared/ ECommerce.Shared/
COPY OrderAPI/ OrderAPI/
RUN dotnet publish OrderAPI/OrderAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "OrderAPI.dll"]
