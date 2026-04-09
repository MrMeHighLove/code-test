FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ProductsApi/ProductsApi.csproj backend/ProductsApi/
RUN dotnet restore backend/ProductsApi/ProductsApi.csproj

COPY backend/ProductsApi/ backend/ProductsApi/
WORKDIR /src/backend/ProductsApi
RUN dotnet publish ProductsApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductsApi.dll"]
