namespace CsCrudApi.Models.UserRelated.Request
{
    public class ChangeEmailRequest
    {
        public string Email { get; set; }
        public string EmailConfirm { get; set; }
    }
}
