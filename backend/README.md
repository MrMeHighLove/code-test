# Backend

## Run locally

1. Create `backend/ProductsApi/.env` from [`backend/ProductsApi/.env.example`](/Users/victormihailov/projects/code-test/backend/ProductsApi/.env.example).
2. Set these values for pure local app-to-app development:

```env
MONGO_CONNECTION_STRING=mongodb://localhost:27017
MONGO_DATABASE_NAME=ProductsDb
JWT_SECRET=your-long-random-secret-at-least-32-characters
JWT_ISSUER=ProductsApi
JWT_AUDIENCE=ProductsClient
FRONTEND_URL=http://localhost:5173
```

3. Make sure MongoDB is running locally on `mongodb://localhost:27017`.
4. Run the API from the repository root:

```bash
dotnet run --project backend/ProductsApi/ProductsApi.csproj
```

The backend runs on `http://localhost:5073` or the port assigned by your local launch profile unless overridden.

## Run tests

From the repository root:

```bash
dotnet test backend/ProductsApi.sln
```

## Run with Docker

From the repository root:

```bash
docker compose up --build
```

The containerized backend runs on `http://localhost:8080`.

For Docker mode, use these backend env values in `backend/ProductsApi/.env`:

```env
MONGO_CONNECTION_STRING=mongodb://mongo:27017
MONGO_DATABASE_NAME=ProductsDb
JWT_SECRET=your-long-random-secret-at-least-32-characters
JWT_ISSUER=ProductsApi
JWT_AUDIENCE=ProductsClient
FRONTEND_URL=http://localhost:4173
```

## Notes

- `FRONTEND_URL` should match the frontend URL for the mode you run:
  `http://localhost:5173` for pure local dev and `http://localhost:4173` for Docker.
- For Docker, the repository already uses the compose wiring and env examples expected by the app.
