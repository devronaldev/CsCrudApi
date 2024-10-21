using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CsCrudApi.Models.UserRelated
{
    [Table("campus_oferece")]
    public class CampusOffer
    {
        public int IdCourse { get; set; }

        public int IdCampus { get; set; }
    }
}
