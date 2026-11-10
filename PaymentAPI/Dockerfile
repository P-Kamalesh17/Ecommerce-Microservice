# Build context must be the solution root:  docker build -f PaymentAPI/Dockerfile -t PaymentAPI .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ECommerce.Shared/ECommerce.Shared.csproj ECommerce.Shared/
COPY PaymentAPI/PaymentAPI.csproj PaymentAPI/
RUN dotnet restore PaymentAPI/PaymentAPI.csproj
COPY ECommerce.Shared/ ECommerce.Shared/
COPY PaymentAPI/ PaymentAPI/
RUN dotnet publish PaymentAPI/PaymentAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "PaymentAPI.dll"]
