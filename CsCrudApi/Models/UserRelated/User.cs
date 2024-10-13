using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated
{
    [Table("usuario")]
    public class User
    {
        [Key]
        [Column("id_usuario")]
        public int IdUser { get; set; }

        [Required]
        [Column("cd_campus")]
        public int CdCampus { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("nome")]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [MaxLength(128)]
        [Column("senha")]
        public string Password { get; set; }

        [Required]
        [Column("data_nascimento")]
        public DateTime DtNasc { get; set; }

        [Required]
        [Column("tipo_preferencia")]
        public EPreferencia TpPreferencia { get; set; }

        [Required]
        [Column("desc_titulo")]
        public ETitulo DescTitulo { get; set; }

        [MaxLength(150)]
        [Column("nome_social")]
        public string? NmSocial { get; set; }

        [Required]
        [Column("tipo_cor")]
        public EColor TpColor { get; set; }

        [Required]
        [Column("cd_cidade")]
        public int CdCidade { get; set; }

        [Required]
        [Column("is_email_verified")]
        public bool IsEmailVerified {  get; set; }
    }

    // Enum for TpPreferencia
    public enum EPreferencia
    {
        Orientar = 1,
        Produzir = 2
    }

    // Enum for DescTitulo
    public enum ETitulo
    {
        Bacharel = 1,
        Mestre = 2,
        Doutor = 3,
        Especialista = 4,
        Tecnologo = 5,
        Licenciado = 6,
        Egresso = 7
    }

    // Enum for TpColor
    public enum EColor
    {
        Dark = 0,
        White = 1,
        Color = 2
    }
}