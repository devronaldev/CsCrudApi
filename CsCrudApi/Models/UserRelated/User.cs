using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated
{
    [Table("usuario")]
    public class User
    {
        [Key]
        [Column("id_usuario")]
        public int UserId { get; set; }

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
        [DataType(DataType.Date)]
        public DateTime DtNasc { get; set; }

        [Required]
        [Column("tipo_interesse")]
        public ETipoInteresse TipoInteresse { get; set; }

        [Required]
        [Column("grau_escolaridade")]
        public EGrauEscolaridade GrauEscolaridade { get; set; }

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

        [Required]
        [Column("status_curso")]
        public EUserStudyStatus StatusCourse { get; set; }
    }

    // Enum for TpPreferencia
    public enum ETipoInteresse
    {
        Orientador = 1,
        Orientado = 2,
        Pesquisa = 3,
        IC = 4
    }

    // Enum for DescTitulo
    public enum EGrauEscolaridade
    {
        EnsinoMedio = 1,
        Graduacao = 2,
        PosGraduacao = 3,
        MBA = 4,
        Mestrado = 5,
        Doutorado = 6,
        PosDoutorado = 7
    }

    // Enum for TpColor
    public enum EColor
    {
        Dark = 1,
        White = 2,
        Color = 3
    }

    public enum EUserStudyStatus
    {
        Active = 0,
        Inactive = 1,
        Concluded = 2
    }
}