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

    /// <summary>La administración desactivó o eliminó al usuario.</summary>
    UserDisabled = 5,

    /// <summary>La administración pidió que el usuario vuelva a crear su contraseña.</summary>
    PasswordResetRequested = 6
}
