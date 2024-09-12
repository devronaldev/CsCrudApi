using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        [Column("iduser")]
        public int Id { get; set; }

        [Required]
        [Column("cd_Campus")]
        public int CampusId { get; set; }

        [Required]
        [Column("nm_user")]
        [MaxLength(150)]
        public string UserName { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [Column("password")]
        [MaxLength(128)]
        public string Password { get; set; }

        [Required]
        [Column("dt_nasc")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Column("tp_preferencia")]
        public UserPreferenceType PreferenceType { get; set; }

        [Required]
        [Column("desc_titulo")]
        public UserTitleType TitleType { get; set; } = UserTitleType.Egresso;

        [Required]
        [Column("nm_social")]
        [MaxLength(150)]
        public string SocialName { get; set; }

        [Required]
        [Column("tp_color")]
        public UserColorType ColorType { get; set; } = UserColorType.WHITE;
    }

    public enum UserPreferenceType
    {
        Orientar,
        Produzir
    }

    public enum UserTitleType
    {
        Bacharel,
        Mestre,
        Doutor,
        Especialista,
        Tecnólogo,
        Licenciado,
        Egresso
    }

    public enum UserColorType
    {
        DARK,
        WHITE,
        COLOR
    }
}