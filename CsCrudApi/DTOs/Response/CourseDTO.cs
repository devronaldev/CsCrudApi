using CsCrudApi.Models.UserRelated.CollegeRelated;

namespace CsCrudApi.DTOs;

public class CourseDTO(Curso c)
{
    public int Id { get; set; } = c.IdCourse;
    public string Name { get; set; } = c.NmCourse;
}