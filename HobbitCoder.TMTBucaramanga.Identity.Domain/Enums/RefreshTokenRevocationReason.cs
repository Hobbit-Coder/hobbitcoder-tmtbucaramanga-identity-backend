namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

public enum RefreshTokenRevocationReason
{
    /// <summary>Se usó y se reemplazó por uno nuevo.</summary>
    Rotated = 1,

    /// <summary>El usuario cerró la sesión.</summary>
    Logout = 2,

    /// <summary>Se presentó un token ya rotado: se revoca toda la sesión.</summary>
    ReuseDetected = 3,

    PasswordChanged = 4,

    /// <summary>Un administrador desactivó o eliminó al usuario.</summary>
    UserDisabled = 5
}
