# DEPLOYMENT.md — Docker, CI/CD & Deployment Architecture
> Local dev with Docker Compose · CI/CD with GitHub Actions · Production on Azure

---

## 1. Port Map (Complete)

| Service | Container Name | Port | Notes |
|---|---|---|---|
| YARP Gateway | gateway | 5000 | Entry point for both frontends |
| Auth.API | auth-api | 5001 | JWT issuance |
| User.API | user-api | 5002 | Profile, wallet, wishlist |
| Catalog.API | catalog-api | 5003 | Products, categories |
| Cart.API | cart-api | 5004 | Shopping cart |
| Order.API | order-api | 5005 | Order lifecycle |
| Admin.API | admin-api | 5009 | CMS, banners, coupons |
| Seller.API | seller-api | 5010 | Seller management |
| Media.API | media-api | 5011 | File uploads |
| SQL Server | sqlserver | 1433 | Database |
| Redis | redis | 6379 | Cache + token blacklist |
| MinIO API | minio | 9000 | Object storage |
| MinIO Console | minio | 9001 | Storage management UI |
| Seq | seq | 5341 | Log viewer (dev only) |
| User Panel | user-panel | 4200 | Customer Angular app |
| Admin Panel | admin-panel | 4201 | Admin Angular app |

---

## 2. docker-compose.yml Structure

```yaml
version: '3.9'

services:
  # ─── Infrastructure ───────────────────────────────────────────
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    environment:
      SA_PASSWORD: ${SQLSERVER_SA_PASSWORD}
      ACCEPT_EULA: Y
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P ${SQLSERVER_SA_PASSWORD} -Q "SELECT 1" -b
      interval: 30s
      timeout: 10s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s

  minio:
    image: minio/minio:latest
    container_name: minio
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin123
    ports:
      - "9000:9000"
      - "9001:9001"
    volumes:
      - minio_data:/data

  seq:
    image: datalust/seq:latest
    container_name: seq
    environment:
      ACCEPT_EULA: Y
    ports:
      - "5341:80"
    volumes:
      - seq_data:/data

  # ─── Backend APIs ──────────────────────────────────────────────
  gateway:
    build:
      context: ./backend
      dockerfile: src/Services/StyleNest.Gateway.API/Dockerfile
    container_name: gateway
    ports:
      - "5000:5000"
    depends_on:
      sqlserver: { condition: service_healthy }
      redis: { condition: service_healthy }
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;...
      - Redis__ConnectionString=redis:6379

  auth-api:
    build:
      context: ./backend
      dockerfile: src/Services/StyleNest.Auth.API/Dockerfile
    container_name: auth-api
    ports:
      - "5001:5001"
    depends_on:
      sqlserver: { condition: service_healthy }
    environment:
      - ConnectionStrings__DefaultConnection=${CONNECTION_STRING}
      - Jwt__PrivateKeyPath=/app/keys/private.pem
      - Jwt__PublicKeyPath=/app/keys/public.pem
      - Jwt__Issuer=${JWT_ISSUER}
      - Jwt__Audience=${JWT_AUDIENCE}
    volumes:
      - ./keys:/app/keys:ro

  # ... (catalog-api, cart-api, order-api, admin-api, seller-api, media-api follow same pattern)

  # ─── Frontend ──────────────────────────────────────────────────
  user-panel:
    build:
      context: ./user-panel
      dockerfile: Dockerfile
    container_name: user-panel
    ports:
      - "4200:80"
    depends_on:
      - gateway

  admin-panel:
    build:
      context: ./admin-panel
      dockerfile: Dockerfile
    container_name: admin-panel
    ports:
      - "4201:80"
    depends_on:
      - gateway

volumes:
  sqlserver_data:
  redis_data:
  minio_data:
  seq_data:
```

---

## 3. Dockerfile Templates

### .NET API Dockerfile (Multi-stage)

```dockerfile
# backend/src/Services/StyleNest.Auth.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/StyleNest.Auth.API/StyleNest.Auth.API.csproj", "Services/StyleNest.Auth.API/"]
COPY ["src/Shared/StyleNest.SharedKernel/StyleNest.SharedKernel.csproj", "Shared/StyleNest.SharedKernel/"]
COPY ["src/Shared/StyleNest.Infrastructure/StyleNest.Infrastructure.csproj", "Shared/StyleNest.Infrastructure/"]
RUN dotnet restore "Services/StyleNest.Auth.API/StyleNest.Auth.API.csproj"
COPY . .
WORKDIR "/src/Services/StyleNest.Auth.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StyleNest.Auth.API.dll"]
```

### Angular Frontend Dockerfile (Multi-stage + Nginx)

```dockerfile
# user-panel/Dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginx:alpine AS final
COPY --from=build /app/dist/user-panel/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Nginx Config (SPA fallback routing)

```nginx
# nginx.conf
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

---

## 4. Environment Variables

### Required Variables (.env)

```bash
# Database
SQLSERVER_SA_PASSWORD=YourStr0ng!Password
SQLSERVER_HOST=sqlserver
SQLSERVER_PORT=1433
SQLSERVER_DB=FashionMarketplaceDb
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=FashionMarketplaceDb;User Id=SA;Password=YourStr0ng!Password;TrustServerCertificate=true

# JWT
JWT_ISSUER=https://stylenest-auth.local
JWT_AUDIENCE=stylenest-spa
JWT_ACCESS_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# Redis
REDIS_CONNECTION=redis:6379

# MinIO (dev)
MINIO_ROOT_USER=minioadmin
MINIO_ROOT_PASSWORD=minioadmin123
MINIO_ENDPOINT=minio:9000

# Seq (dev)
SEQ_URL=http://seq:5341
```

### Multi-Environment File Strategy

```
.env                    ← Git-ignored. Local dev values.
.env.example            ← Git-committed. Template only (no secrets).
.env.staging            ← Git-ignored. Staging values.
appsettings.json        ← Git-committed. Non-secret production defaults.
appsettings.Development.json  ← Git-ignored. Dev overrides.
appsettings.Staging.json      ← Git-ignored. Staging overrides.
```

---

## 5. GitHub Actions CI/CD

### CI Pipeline (`.github/workflows/ci.yml`)

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Build
        run: dotnet build backend/stylenest-clone.slnx --configuration Release

      - name: Test
        run: dotnet test backend/stylenest-clone.slnx --configuration Release --no-build

  admin-panel:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node 22
        uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: admin-panel/package-lock.json

      - name: Install
        run: cd admin-panel && npm ci

      - name: Type check
        run: cd admin-panel && npx tsc --noEmit

      - name: Build production
        run: cd admin-panel && npx ng build --configuration production

  user-panel:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node 22
        uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: user-panel/package-lock.json

      - name: Install
        run: cd user-panel && npm ci

      - name: Type check
        run: cd user-panel && npx tsc --noEmit

      - name: Build production
        run: cd user-panel && npx ng build --configuration production

  docker-build:
    needs: [backend, admin-panel, user-panel]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Login to ACR
        uses: docker/login-action@v3
        with:
          registry: ${{ secrets.ACR_REGISTRY }}
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}

      - name: Build and push all images
        run: |
          docker compose build
          docker compose push
```

### Deployment Pipeline (`.github/workflows/deploy.yml`)

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ secrets.AZURE_APP_NAME }}
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          images: ${{ secrets.ACR_REGISTRY }}/auth-api:${{ github.sha }}
```

---

## 6. Production Architecture (Azure)

```
Azure Resource Group: fashion-marketplace-prod
│
├── Azure App Service Plan (P2v3)    ← Host all microservices
│   ├── auth-api.azurewebsites.net
│   ├── user-api.azurewebsites.net
│   ├── catalog-api.azurewebsites.net
│   ├── cart-api.azurewebsites.net
│   ├── order-api.azurewebsites.net
│   ├── admin-api.azurewebsites.net
│   ├── seller-api.azurewebsites.net
│   └── media-api.azurewebsites.net
│
├── Azure Static Web Apps
│   ├── www.yourdomain.com          ← User Storefront
│   └── admin.yourdomain.com        ← Admin Panel
│
├── Azure SQL Database (S2 tier)
│   └── Geo-redundant backup enabled
│
├── Azure Cache for Redis (C1)
│
├── Azure Blob Storage
│   └── fashion-media (container)
│
├── Azure CDN
│   └── cdn.yourdomain.com → Azure Blob
│
├── Azure Container Registry
│   └── All Docker images stored here
│
├── Azure Application Insights
│   └── All services send telemetry
│
└── Azure Key Vault
    └── All secrets (connection strings, JWT keys, etc.)
```

---

## 7. RSA Key Generation (JWT RS256)

```bash
# Generate RSA private key
openssl genrsa -out keys/private.pem 2048

# Extract public key
openssl rsa -in keys/private.pem -pubout -out keys/public.pem

# Verify
openssl rsa -in keys/private.pem -check
```

**Auth.API** needs: `private.pem` + `public.pem`
**All other APIs** need: `public.pem` only (for verification, never signing)

Keys are stored in `keys/` directory (gitignored) and mounted into containers as read-only volumes.

---

## 8. Local Dev Quick Start

```bash
# 1. Clone the repo
git clone <repo-url>
cd fashion-marketplace

# 2. Generate RSA keys
mkdir keys
openssl genrsa -out keys/private.pem 2048
openssl rsa -in keys/private.pem -pubout -out keys/public.pem

# 3. Copy environment template
cp .env.example .env
# Edit .env: set SQLSERVER_SA_PASSWORD to something strong

# 4. Start the full stack
docker compose up --build

# 5. Wait for health checks, then access:
# User Storefront:  http://localhost:4200
# Admin Panel:      http://localhost:4201
# API Gateway:      http://localhost:5000
# MinIO Console:    http://localhost:9001  (minioadmin/minioadmin123)
# Seq Logs:         http://localhost:5341
```

---

## 9. Scaling Strategy

### Phase 1 (Current — Single Host)

All containers on one Docker Compose host. Suitable for development and low-traffic staging.

### Phase 3 (Scale-Up)

```
Azure App Service → scale-up to P3v3 tier
Azure SQL → scale-up to S4 tier
Redis → C2 tier (bigger instance)
```

### Phase 5 (Scale-Out)

```
AKS (Azure Kubernetes Service):
  - Horizontal Pod Autoscaler on all API deployments
  - Scale on CPU > 70% or request rate > X RPS
  - Separate node pools for APIs vs workers (Hangfire)

Azure Application Gateway (Layer 7):
  - Replace YARP with Azure App Gateway + WAF
  - Blue/green deployment support via traffic weights
```

---

*See [SECURITY.md](SECURITY.md) for security hardening steps.*
*See [PERFORMANCE.md](PERFORMANCE.md) for caching and optimization.*
