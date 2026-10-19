namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>Estado del acceso de la cuenta, derivado de su activación y de su contraseña.</summary>
public enum AccessStatus
{
    /// <summary>El titular todavía no completa el primer ingreso.</summary>
    PendingActivation = 1,

    /// <summary>Puede ingresar con su contraseña.</summary>
    Ready = 2,

    /// <summary>La administración pidió que cree una contraseña nueva.</summary>
    PasswordResetRequested = 3
}
