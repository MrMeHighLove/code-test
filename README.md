# Code Test

## Backend setup locally

1. Create `backend/ProductsApi/.env`.
2. Set:

```env
MONGO_CONNECTION_STRING=mongodb://localhost:27017
MONGO_DATABASE_NAME=ProductsDb
JWT_SECRET=your-long-random-secret-at-least-32-characters
JWT_ISSUER=ProductsApi
JWT_AUDIENCE=ProductsClient
FRONTEND_URL=http://localhost:5173
```

3. Make sure MongoDB is running locally on `mongodb://localhost:27017`.
4. Start the backend from the repository root:

```bash
dotnet run --project backend/ProductsApi/ProductsApi.csproj
```

The backend runs locally on `http://localhost:5073`.

## Frontend setup locally

1. Create `frontend/.env`.
2. Set:

```env
VITE_API_BASE_URL=http://localhost:5073
```

3. Install dependencies:

```bash
cd frontend
npm install
```

4. Start the frontend:

```bash
npm run dev
```

The frontend runs locally on `http://localhost:5173`.
