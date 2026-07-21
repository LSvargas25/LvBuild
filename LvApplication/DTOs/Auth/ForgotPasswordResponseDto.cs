namespace LvApplication.DTOs.Auth;

public class ForgotPasswordResponseDto
{
    public string Message { get; set; } = "Si el correo existe en el sistema, se enviarán instrucciones para restablecer la contraseña.";
    public string? ResetToken { get; set; }
}
