namespace ProductsApi.Constants;

public static class ApiConstants
{
    public static class Environment
    {
        public const string MongoConnectionString = "MONGO_CONNECTION_STRING";
        public const string MongoDatabaseName = "MONGO_DATABASE_NAME";
        public const string JwtSecret = "JWT_SECRET";
        public const string JwtIssuer = "JWT_ISSUER";
        public const string JwtAudience = "JWT_AUDIENCE";
        public const string FrontendUrl = "FRONTEND_URL";
    }

    public static class Defaults
    {
        public const string MongoConnectionString = "mongodb://localhost:27017";
        public const string MongoDatabaseName = "ProductsDb";
        public const string JwtIssuer = "ProductsApi";
        public const string JwtAudience = "ProductsClient";
        public const string FrontendUrl = "http://localhost:5173";
        public const string ApplicationName = "ProductsApi";
        public const string DataProtectionDirectory = "/var/app/data-protection";
        public const string GlobalExceptionLogger = "GlobalExceptionHandler";
        public const string CorsPolicy = "Frontend";
        public const string AccessTokenCookie = "access_token";
        public const string RefreshTokenCookie = "refresh_token";
        public const string JsonContentType = "application/json";
        public const string CacheControlNoCache = "private, no-cache";
        public const string AnonymousUser = "anonymous";
        public const int RetryAfterSeconds = 2;
    }

    public static class Errors
    {
        public const string JwtSecretRequired = "JWT_SECRET is required.";
        public const string UnexpectedError = "An unexpected error occurred.";
        public const string NoRefreshTokenProvided = "No refresh token provided.";
        public const string LoggedOutSuccessfully = "Logged out successfully.";
        public const string ProductNotFound = "Product not found.";
        public const string IdempotencyPayloadMismatch = "The same Idempotency-Key cannot be reused with a different payload.";
        public const string IdempotencyInProgress = "A request with this Idempotency-Key is already in progress.";
    }

    public static class AuditEvents
    {
        public const string AuthRegister = "auth.register";
        public const string AuthLogin = "auth.login";
        public const string AuthRefresh = "auth.refresh";
        public const string AuthLogout = "auth.logout";
        public const string ProductCreate = "product.create";
    }

    public static class AuditOutcomes
    {
        public const string Success = "Success";
        public const string Failure = "Failure";
    }

    public static class Headers
    {
        public const string IdempotencyKey = "Idempotency-Key";
        public const string IdempotentReplayed = "X-Idempotent-Replayed";
    }

    public static class Routes
    {
        public const string RefreshTokenPath = "/api/auth";
    }
}
