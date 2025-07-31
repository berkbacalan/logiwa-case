namespace EcomMMS.API.Configuration
{
    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; } = new();
        public Redis Redis { get; set; } = new();
        public Seq Seq { get; set; } = new();
        public RateLimiting RateLimiting { get; set; } = new();
        public ApiVersioning ApiVersioning { get; set; } = new();
        public Cors Cors { get; set; } = new();
    }

    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; } = string.Empty;
    }

    public class Redis
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string InstanceName { get; set; } = "EcomMMS_";
        public int DefaultExpirationHours { get; set; } = 1;
    }

    public class Seq
    {
        public string ServerUrl { get; set; } = string.Empty;
    }

    public class RateLimiting
    {
        public int PermitLimit { get; set; } = 100;
        public int Window { get; set; } = 60;
        public int SegmentsPerWindow { get; set; } = 1;
    }

    public class ApiVersioning
    {
        public string DefaultVersion { get; set; } = "1.0";
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;
        public bool ReportApiVersions { get; set; } = true;
    }

    public class Cors
    {
        public string[] AllowedOrigins { get; set; } = { "http://localhost:3000", "http://localhost:4200" };
        public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        public string[] AllowedHeaders { get; set; } = { "Content-Type", "Authorization" };
        public bool AllowCredentials { get; set; } = true;
    }
} 