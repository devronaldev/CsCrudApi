using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated
{
    [Table("usuario_estuda_curso")]
    public class UserStudyCourse
    {
        [Required]
        public int IdUser { get; set; }

        [Required]
        public int IdCourse { get; set; }

        [Required]
        public EUserStudyStatus UserStudyStatus { get; set; }
    }

    public enum EUserStudyStatus {
        Active = 0,
        Inactive = 1,
        Concluded = 2 
    }
}