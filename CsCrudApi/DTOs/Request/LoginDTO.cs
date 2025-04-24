namespace CsCrudApi.Models.UserRelated.Request
{
    public class LoginDTO
    {
        /// <summary>
        /// E-mail do usuário.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Senha do usuário.
        /// </summary>
        public string Password { get; set; }
    }
}
