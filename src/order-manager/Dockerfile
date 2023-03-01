#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["order-manager/order-manager.csproj", "order-manager/"]
COPY ["trading-model/trading-model.csproj", "trading-model/"]
RUN dotnet restore "order-manager/order-manager.csproj"
COPY . .
WORKDIR "/src/order-manager"
RUN dotnet build "order-manager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "order-manager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true