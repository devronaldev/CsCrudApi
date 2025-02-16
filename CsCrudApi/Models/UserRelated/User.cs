﻿using CsCrudApi.Models.UserRelated.CollegeRelated;
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

        [Required]
        public int CursoId { get; set; }

        [Required]
        [Column("url_foto_perfil")]
        public string? ProfilePictureUrl { get; set; }
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
        EnsinoMedio = 0,
        Graduacao = 1,
        PosGraduacao = 2,
        MBA = 3,
        Mestrado = 4,
        Doutorado = 5,
        PosDoutorado = 6
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