using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated
{
    [Table("EmailVerification")]
    public class EmailVerification
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("novo_email")]
        public string NewEmail {  get; set; }

        [Required]
        [Column("verification_token")]
        public string VerificationToken { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Required]
        [Column("is_verified")]
        public bool IsVerified { get; set; }
    }
}
