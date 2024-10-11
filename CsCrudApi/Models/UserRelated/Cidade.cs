using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated
{
    [Table("cidade")]
    public class Cidade
    {
        [Key]
        [Column("id_cidade")]
        public int IdCidade { get; set; }

        [Column("nome")]
        public string Name { get; set; }

        [Column("estado")]
        public EEstados Estado { get; set; }

        [Column("cod_ibge")]
        public int CodIBGE { get; set; }
    }

    public enum EEstados
    {
        SP = 1,  // São Paulo
        MG = 2,  // Minas Gerais
        RJ = 3,  // Rio de Janeiro
        BA = 4,  // Bahia
        PR = 5,  // Paraná
        RS = 6,  // Rio Grande do Sul
        PE = 7,  // Pernambuco
        CE = 8,  // Ceará
        PA = 9,  // Pará
        SC = 10, // Santa Catarina
        MA = 11, // Maranhão
        GO = 12, // Goiás
        AM = 13, // Amazonas
        PB = 14, // Paraíba
        ES = 15, // Espírito Santo
        RN = 16, // Rio Grande do Norte
        AL = 17, // Alagoas
        PI = 18, // Piauí
        MT = 19, // Mato Grosso
        DF = 20, // Distrito Federal
        MS = 21, // Mato Grosso do Sul
        SE = 22, // Sergipe
        RO = 23, // Rondônia
        TO = 24, // Tocantins
        AC = 25, // Acre
        AP = 26, // Amapá
        RR = 27  // Roraima
    }
}
