using CsCrudApi.DTOs;
using CsCrudApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public CourseController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Retorna uma lista de todos os cursos disponíveis no sistema.
        /// </summary>
        /// <remarks>
        /// Os cursos são ordenados alfabeticamente pelo nome.
        /// Este endpoint não requer autenticação (AllowAnonymous) e exige conexão HTTPS (RequireHttps).
        /// </remarks>
        /// <response code="200">Retorna uma lista de objetos CourseDTO se houver cursos.</response>
        /// <response code="204">Retorna No Content se não houver cursos disponíveis.</response>
        /// <response code="500">Retorna um objeto ErrorDTO em caso de exceção interna do servidor.</response>
        [ProducesResponseType(typeof(CourseDTO), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        [HttpGet]
        [AllowAnonymous]
        //[RequireHttps]
        public async Task<ActionResult<List<CourseDTO>>> GetCursos()
        {
            try
            {
                var cursos = await _context.Cursos
                    .OrderBy(c => c.NmCourse)
                    .Select(c => new CourseDTO(c))
                    .ToListAsync();

                if (!cursos.Any())
                {
                    return NoContent();
                }
                return Ok(cursos);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro ao listar cursos: {e.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTCOURSES",
                    ErrorDescription = "Exceção não tratada ocorreu.",
                    Message = "Um erro inesperado ocorreu ao tentar listar os cursos."
                });
            }
        }

        /// <summary>
        /// Retorna um curso específico pelo seu ID.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de detalhes de um único curso com base no seu identificador.
        /// </remarks>
        /// <param name="id">O ID numérico do curso a ser recuperado.</param>
        /// <response code="200">Retorna o objeto CourseDTO do curso encontrado.</response>
        /// <response code="400">Retorna um ErrorDTO se o ID fornecido for inválido (menor que 0).</response>
        /// <response code="404">Retorna um ErrorDTO se nenhum curso com o ID especificado for encontrado.</response>
        /// <response code="500">Retorna um ErrorDTO em caso de exceção interna do servidor.</response>
        [AllowAnonymous]
        //[RequireHttps]
        [ProducesResponseType(typeof(CourseDTO), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDTO>> GetCurso([FromRoute] int id)
        {
            try
            {
                if (id < 0)
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDID",
                        ErrorDescription = "O ID do curso não pode ser negativo.",
                        Message = "O ID fornecido para o curso é inválido."
                    });
                }

                var course = await _context.Cursos
                    .FirstOrDefaultAsync(course => course.IdCourse == id);
                if (course == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404COURSENOTFOUND",
                        ErrorDescription = $"Nenhum curso com o ID {id} foi encontrado.",
                        Message = "O curso solicitado não foi encontrado."
                    });
                }

                return Ok(new CourseDTO(course));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erro ao obter curso por ID: {e.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500GETCOURSE",
                    ErrorDescription = $"Exceção não tratada ocorreu ao buscar curso com ID: {id}",
                    Message = "Um erro inesperado ocorreu ao tentar obter o curso."
                });
            }
        }
    }    
}
