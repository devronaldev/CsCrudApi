using System.ComponentModel.DataAnnotations;

namespace CsCrudApi.DTOs
{
    /// <summary>
    /// Modelo para a requisição de solicitação de atualização de e-mail.
    /// </summary>
    public record ChangeEmailRequest
    {
        /// <summary>
        /// O novo endereço de e-mail desejado.
        /// </summary>
        /// <example>novo.email@example.com</example>
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        public string Email { get; set; }

        /// <summary>
        /// Confirmação do novo endereço de e-mail. Deve ser idêntico ao campo Email.
        /// </summary>
        /// <example>novo.email@example.com</example>
        [Required(ErrorMessage = "A confirmação do e-mail é obrigatória.")]
        [Compare("Email", ErrorMessage = "O e-mail e a confirmação do e-mail não coincidem.")]
        public string EmailConfirm { get; set; }
    }
}
