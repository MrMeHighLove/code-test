# Products API — Design Spec

## Overview

Full-stack Products application: .NET 10 Web API backend with MongoDB, React (Vite + TypeScript) frontend. Cookie-based JWT authentication with short-lived access tokens and long-lived refresh tokens.

## Backend (`/backend`)

### Tech Stack
- .NET 10.0 Web API (minimal APIs or controllers)
- MongoDB via `MongoDB.Driver` (no Entity Framework)
- JWT auth with HttpOnly cookies
- xUnit + WebApplicationFactory + Moq for testing
- `DotNetEnv` for `.env` file loading

### Data Models

**Product**
| Field       | Type     | Notes                    |
|-------------|----------|--------------------------|
| Id          | string   | MongoDB ObjectId as string |
| Name        | string   | Required                 |
| Description | string   | Optional                 |
| Price       | decimal  | Required, > 0            |
| Colour      | string   | Required                 |
| CreatedAt   | DateTime | Auto-set on creation     |

**User**
| Field        | Type     | Notes                    |
|--------------|----------|--------------------------|
| Id           | string   | MongoDB ObjectId as string |
| Username     | string   | Required, unique         |
| PasswordHash | string   | PBKDF2 hashed            |
| CreatedAt    | DateTime | Auto-set on creation     |

### API Endpoints

| Method | Path               | Auth     | Description                          |
|--------|--------------------|----------|--------------------------------------|
| GET    | /api/health        | Anonymous| Returns `{ "status": "ok" }`         |
| POST   | /api/auth/register | Anonymous| Create user account                  |
| POST   | /api/auth/login    | Anonymous| Login, sets JWT cookies              |
| POST   | /api/auth/refresh  | Cookie   | Rotate access + refresh tokens       |
| POST   | /api/auth/logout   | Cookie   | Clear auth cookies                   |
| GET    | /api/products      | JWT      | List products, optional `?colour=X`  |
| POST   | /api/products      | JWT      | Create a product                     |

### Authentication Flow
1. User registers via `/api/auth/register`
2. User logs in via `/api/auth/login` with username/password
3. Server validates credentials, returns two HttpOnly cookies:
   - `access_token` — JWT, 15-minute expiry
   - `refresh_token` — opaque token, 7-day expiry, stored in MongoDB
4. Protected endpoints validate the `access_token` cookie
5. When access token expires, client calls `/api/auth/refresh` which reads the refresh token cookie, validates it, and issues new pair
6. Logout clears both cookies and invalidates the refresh token in MongoDB

### Configuration (`.env`)
```
MONGO_CONNECTION_STRING=mongodb://localhost:27017
MONGO_DATABASE_NAME=ProductsDb
JWT_SECRET=<generated-secret>
JWT_ISSUER=ProductsApi
JWT_AUDIENCE=ProductsClient
```

### Project Structure
```
backend/
  ProductsApi/
    Program.cs
    Models/
    Services/
    Controllers/
    Configuration/
  ProductsApi.Tests/
    Unit/
    Integration/
```

## Frontend (`/frontend`)

### Tech Stack
- React 19 + TypeScript + Vite
- Axios for HTTP (cookies sent automatically with `withCredentials`)
- React Router for navigation

### Pages
- **Login** — username/password form
- **Register** — username/password form
- **Products List** — table/grid with colour filter dropdown
- **Create Product** — form with Name, Description, Price, Colour

### Configuration (`.env`)
```
VITE_API_BASE_URL=http://localhost:5000
```

## Architecture Diagram

Mermaid diagram in `docs/architecture.md` showing:
- Products Service (this project)
- Orders Service
- Payments Service
- Notification Service
- Event Bus (RabbitMQ/Kafka)
- API Gateway
- MongoDB instances per service

## Testing Strategy

**Unit Tests**: Service layer methods mocked at the repository/MongoDB level using interfaces + Moq.

**Integration Tests**: `WebApplicationFactory` with a test MongoDB instance (or mocked `IMongoCollection` via interface). Tests cover auth flow and product CRUD including colour filtering.
