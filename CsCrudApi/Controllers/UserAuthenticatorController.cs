using CsCrudApi.Services;
using CsCrudApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CsCrudApi.Repository;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public UserAuthController(ApplicationDbContext context, IConfiguration configuration) 
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<dynamic>> Login([FromBody] User model)
        {
            var user = UserRepository.Get(model.Email, model.Password); 

            if (user == null) 
            {
                return NotFound(new
                {
                    message = "Usuário ou senha inexistente"
                });
            }
            var token = Services.TokenServices.GenerateToken(user);
            user.Password = "";
            return new 
            {
                user,
                token
            };
        }
    }
}
