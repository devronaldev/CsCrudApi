using CsCrudApi.Models;
namespace CsCrudApi.Repository
{
    public static class UserRepository
    {
        public static User Get(string email, string password)
        {
            var users = new List<User>();
            users.Add(new User { Id = 1, Name = "Ronald Evangelista", Password = "Vasco$777", Email = "ronald.evangelista@aluno.ifsp.edu.br" });
            users.Add(new User { Id = 2, Name = "Ariel Martins", Password = "SantosB24$", Email = "ariel.martins@aluno.ifsp.edu.br" });
            return users.FirstOrDefault(x => x.Email == email && x.Password == password);
        }
    }
}
