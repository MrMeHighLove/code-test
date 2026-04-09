# Frontend

## Run locally

1. Create `frontend/.env` from [`frontend/.env.example`](/Users/victormihailov/projects/code-test/frontend/.env.example).
2. Set this value for pure local frontend talking to a pure local backend:

```env
VITE_API_BASE_URL=http://localhost:5073
```

3. Install dependencies:

```bash
npm install
```

4. Start the app:

```bash
npm run dev
```

The frontend runs on `http://localhost:5173` in local dev.

## Run with Docker

From the repository root:

```bash
docker compose up --build frontend
```

The containerized frontend runs on `http://localhost:4173`.

For Docker mode, set this in `frontend/.env`:

```env
VITE_API_BASE_URL=http://localhost:8080
```

## Notes

- The frontend expects the backend API to be available at the URL configured in `VITE_API_BASE_URL`.
- Use `http://localhost:5073` when both frontend and backend run locally, and `http://localhost:8080` when the backend runs through Docker.
- Authentication uses cookies, so the frontend and backend URLs should match the setup described in the backend README.
