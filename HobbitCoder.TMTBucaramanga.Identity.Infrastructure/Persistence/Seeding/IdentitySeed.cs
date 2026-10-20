using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Seeding;

/// <summary>
/// Cuentas iniciales del ministerio 2026: directores, coordinadores y líderes de grupo. La genera
/// <c>docs/resources/bases-grupos/generar-semillas.py</c> a partir de los Excel y viaja incrustada en el
/// ensamblado. Sus Id son los que usa la carga inicial de Sheepfold para las responsabilidades de grupo.
/// </summary>
public class IdentitySeed
{
    public const string ResourceName = "IdentitySeed.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public List<User> Users { get; set; } = [];

    public static IdentitySeed Load()
    {
        using var stream = typeof(IdentitySeed).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"No se encontró el recurso incrustado {ResourceName}.");

        return JsonSerializer.Deserialize<IdentitySeed>(stream, Options)
            ?? throw new InvalidOperationException($"{ResourceName} está vacío.");
    }
}
