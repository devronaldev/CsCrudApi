using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace CsCrudApi.Models.User
{
    [Table("User")]
    public class User
    {
        [Key]
        public int IdUser { get; set; }

        [Required]
        public int CdCampus { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("nmUser")]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [MaxLength(128)]
        [Column("password")]
        public string Password { get; set; }

        [Required]
        public DateTime DtNasc { get; set; }

        [Required]
        public Preferencia TpPreferencia { get; set; }

        [Required]
        public Titulo DescTitulo { get; set; }

        [Required]
        [MaxLength(150)]
        public string NmSocial { get; set; }

        [Required]
        public Color TpColor { get; set; }

        [Required]
        public bool IsEmailVerified {  get; set; }
    }

    // Enum for TpPreferencia
    public enum Preferencia
    {
        Orientar = 1,
        Produzir = 2
    }

    // Enum for DescTitulo
    public enum Titulo
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
    public enum Color
    {
        Dark = 0,
        White = 1,
        Color = 2
    }
}