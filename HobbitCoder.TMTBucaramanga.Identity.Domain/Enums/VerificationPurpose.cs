namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

/// <summary>Proceso de seguridad que confirma la identidad del titular.</summary>
public enum VerificationPurpose
{
    /// <summary>Primer ingreso: la cuenta aún no se ha activado.</summary>
    Activation = 1,

    /// <summary>Recuperar el acceso o crear la contraseña después de un reinicio solicitado por la administración.</summary>
    Recovery = 2
}
