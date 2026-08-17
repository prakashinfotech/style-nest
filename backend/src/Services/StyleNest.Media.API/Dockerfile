FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/StyleNest.Media.API/StyleNest.Media.API.csproj", "src/Services/StyleNest.Media.API/"]
COPY ["src/Shared/StyleNest.Infrastructure/StyleNest.Infrastructure.csproj", "src/Shared/StyleNest.Infrastructure/"]
COPY ["src/Shared/StyleNest.SharedKernel/StyleNest.SharedKernel.csproj", "src/Shared/StyleNest.SharedKernel/"]
RUN dotnet restore "src/Services/StyleNest.Media.API/StyleNest.Media.API.csproj"

COPY . .
WORKDIR "/src/src/Services/StyleNest.Media.API"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/uploads
VOLUME ["/app/uploads"]

COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "StyleNest.Media.API.dll"]
