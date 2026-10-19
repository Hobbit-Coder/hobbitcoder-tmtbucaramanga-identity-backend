namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;

/// <summary>Fecha local del ministerio (America/Bogota, sin horario de verano: UTC-5).</summary>
public static class LocalCalendar
{
    private static readonly TimeZoneInfo Bogota = FindBogota();

    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Bogota).DateTime);
    }

    /// <summary>
    ///     Obtiene la zona horaria de Bogotá.
    /// </summary>
    /// <remarks>
    ///     Imágenes de contenedor sin base de zonas horarias: Colombia no tiene horario de verano.
    /// </remarks>
    private static TimeZoneInfo FindBogota()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("America/Bogota", TimeSpan.FromHours(-5), "America/Bogota", "America/Bogota");
        }
    }
}
