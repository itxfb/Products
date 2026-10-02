FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY .editorconfig Directory.Build.props Directory.Packages.props ./
COPY src/Products.Api/Products.Api.csproj src/Products.Api/
RUN dotnet restore src/Products.Api/Products.Api.csproj
COPY src/Products.Api/ src/Products.Api/
RUN dotnet publish src/Products.Api/Products.Api.csproj -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Products.Api.dll"]
