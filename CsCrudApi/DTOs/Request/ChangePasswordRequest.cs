using System.ComponentModel.DataAnnotations;

namespace CsCrudApi.Models.UserRelated.Request
{
    /// <summary>
    /// Representa os dados necessários para a requisição de alteração de senha do usuário.
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// A senha atual do usuário.
        /// </summary>
        [Required(ErrorMessage = "A senha antiga é obrigatória.")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// A nova senha do usuário. Deve atender aos requisitos de segurança.
        /// </summary>
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        // [StringLength(100, MinimumLength = 8, ErrorMessage = "A nova senha deve ter entre 8 e 100 caracteres.")]
        // [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$", ErrorMessage = "A senha não atende aos requisitos de segurança.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirmação da nova senha, deve ser idêntica à <see cref="NewPassword"/>.
        /// </summary>
        [Required(ErrorMessage = "A confirmação da nova senha é obrigatória.")]
        [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação não correspondem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}
