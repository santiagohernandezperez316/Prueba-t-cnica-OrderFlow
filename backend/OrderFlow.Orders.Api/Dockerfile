FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["nuget.config", "global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["OrderFlow.Contracts/OrderFlow.Contracts.csproj", "OrderFlow.Contracts/"]
COPY ["OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj", "OrderFlow.Orders.Api/"]
RUN dotnet restore "OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj"
COPY . .
RUN dotnet publish "OrderFlow.Orders.Api/OrderFlow.Orders.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# curl only for the docker compose healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
# Non-root user shipped with the .NET images; listens on 8080 (ASPNETCORE_HTTP_PORTS).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderFlow.Orders.Api.dll"]
