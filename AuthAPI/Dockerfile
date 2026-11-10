# Build context must be the solution root:  docker build -f AuthAPI/Dockerfile -t AuthAPI .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Shared/ECommerce.Shared.csproj ECommerce.Shared/
COPY AuthAPI/AuthAPI.csproj AuthAPI/
RUN dotnet restore AuthAPI/AuthAPI.csproj
COPY ECommerce.Shared/ ECommerce.Shared/
COPY AuthAPI/ AuthAPI/
RUN dotnet publish AuthAPI/AuthAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "AuthAPI.dll"]
