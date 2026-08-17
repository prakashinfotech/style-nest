FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.Config .
COPY src/Shared/StyleNest.SharedKernel/StyleNest.SharedKernel.csproj       src/Shared/StyleNest.SharedKernel/
COPY src/Shared/StyleNest.Infrastructure/StyleNest.Infrastructure.csproj   src/Shared/StyleNest.Infrastructure/
COPY src/Services/StyleNest.Seller.API/StyleNest.Seller.API.csproj          src/Services/StyleNest.Seller.API/
RUN dotnet restore src/Services/StyleNest.Seller.API/StyleNest.Seller.API.csproj

COPY src/Shared/StyleNest.SharedKernel/    src/Shared/StyleNest.SharedKernel/
COPY src/Shared/StyleNest.Infrastructure/  src/Shared/StyleNest.Infrastructure/
COPY src/Services/StyleNest.Seller.API/    src/Services/StyleNest.Seller.API/

RUN dotnet publish src/Services/StyleNest.Seller.API/StyleNest.Seller.API.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "StyleNest.Seller.API.dll"]
