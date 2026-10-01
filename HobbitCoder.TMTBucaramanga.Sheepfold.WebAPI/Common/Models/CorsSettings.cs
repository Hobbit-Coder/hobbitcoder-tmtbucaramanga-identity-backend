namespace HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.Common.Models;

public class CorsSettings
{
    public required string PolicyName { get; set; }
    public List<string> AllowedOrigins { get; set; } = [];
    public bool AllowAnyOrigin { get; set; }
    public bool AllowCredentials { get; set; }
    public List<string> AllowedMethods { get; set; } = [];
    public List<string> AllowedHeaders { get; set; } = [];
    public List<string> ExposedHeaders { get; set; } = [];
    public int MaxAgeInSeconds { get; set; }
}
