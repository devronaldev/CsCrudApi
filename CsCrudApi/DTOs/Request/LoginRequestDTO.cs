namespace CsCrudApi.DTOs
{
    public class LoginRequestDTO
    {
        /// <summary>
        /// E-mail do usuário.
        /// </summary>
        /// <example>seu_email@dominio.com</example>
        public string Email { get; set; }

        /// <summary>
        /// Senha do usuário.
        /// </summary>
        /// <example>sua_senha</example>
        public string Password { get; set; }
    }
}
