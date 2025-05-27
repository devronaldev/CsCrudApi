using CsCrudApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using CsCrudApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CsCrudApi.Models.UserRelated.CollegeRelated;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LocationController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Lista todas as cidades cadastradas.
        /// </summary>
        /// <remarks>
        /// Este endpoint retorna uma lista completa de cidades disponíveis no sistema.
        /// Ideal para populações de dropdowns ou seletores.
        /// </remarks>
        /// <returns>Uma lista de objetos <see cref="CityResponseDTO"/> representando as cidades.</returns>
        /// <response code="200">Retorna a lista de cidades com sucesso.</response>
        /// <response code="204">Não há cidades cadastradas.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("cidades")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CityResponseDTO>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<CityResponseDTO>>> ListarCidades()
        {
            try
            {
                var cidades = await _context.Cidades
                    .Select(c => new CityResponseDTO(c))
                    .ToListAsync();

                if (cidades == null || !cidades.Any())
                {
                    return NoContent();
                }

                return Ok(cidades);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao listar cidades: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTCIDADES",
                    Message = "Não foi possível listar as cidades.",
                    ErrorDescription = $"Ocorreu um erro inesperado ao buscar as cidades."
                });
            }
        }

        /// <summary>
        /// Lista todos os campi cadastrados no sistema.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de uma lista completa de todos os campi disponíveis.
        /// As informações retornadas incluem detalhes como ID, sigla, nome e a cidade associada ao campus.
        /// </remarks>
        /// <returns>Uma lista de objetos <see cref="CampusResponseDTO"/> representando os campi.</returns>
        /// <response code="200">Retorna a lista de campi com sucesso.</response>
        /// <response code="204">Não há campi cadastradas no sistema.</response>
        /// <response code="500">Ocorreu um erro interno no servidor ao tentar listar os campi.</response>
        [HttpGet("campi")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CampusResponseDTO>), 200)] 
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<CampusResponseDTO>>> ListarCampi()
        {
            try
            {
                var campi = await _context.Campi
                                          .Select(c => new CampusResponseDTO(c)) 
                                          .ToListAsync();

                if (campi == null || !campi.Any())
                {
                    return NoContent();
                }
                return Ok(campi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao listar campi: {ex.Message}");

                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTCAMPI",
                    Message = "Não foi possível listar os campi.",
                    ErrorDescription = $"Ocorreu um erro inesperado ao buscar os campi."
                });
            }
        }

        /// <summary>
        /// Lista todas as áreas de estudo cadastradas.
        /// </summary>
        /// <remarks>
        /// Este endpoint retorna uma lista completa de áreas de estudo disponíveis no sistema.
        /// </remarks>
        /// <returns>Uma lista de objetos <see cref="Area"/> representando as áreas.</returns>
        /// <response code="200">Retorna a lista de áreas com sucesso.</response>
        /// <response code="204">Não há áreas cadastradas.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("areas")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<Area>), 200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<Area>>> ListAreas()
        {
            try
            {
                var areas = await _context.Areas
                    .ToListAsync();

                if (areas == null || !areas.Any())
                {
                    return NoContent();
                }

                return Ok(areas);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao listar áreas: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTAREAS",
                    Message = "Não foi possível listar as áreas.",
                    ErrorDescription = $"Ocorreu um erro inesperado ao buscar as áreas."
                });
            }
        }

        /// <summary>
        /// Obtém os detalhes de uma cidade específica pelo seu ID.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de informações detalhadas de uma única cidade
        /// utilizando seu identificador único.
        /// </remarks>
        /// <param name="id">O ID único da cidade a ser pesquisada.</param>
        /// <returns>Os detalhes da cidade encontrada.</returns>
        /// <response code="200">Retorna os detalhes da cidade com sucesso.</response>
        /// <response code="400">O ID da cidade fornecido é inválido.</response>
        /// <response code="404">Nenhuma cidade foi encontrada com o ID especificado.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("cidade/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CityResponseDTO), 200)] 
        [ProducesResponseType(typeof(ErrorDTO), 400)] 
        [ProducesResponseType(typeof(ErrorDTO), 404)] 
        [ProducesResponseType(typeof(ErrorDTO), 500)] 
        public async Task<ActionResult<CityResponseDTO>> GetCityById([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    var errorBadRequest = new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDID",
                        Message = "ID da cidade inválido.",
                        ErrorDescription = "O ID fornecido para a cidade deve ser um número inteiro positivo."
                    };
                    return BadRequest(errorBadRequest);
                }
                
                var cidade = await _context.Cidades
                                            .Select(c => new CityResponseDTO(c))
                                            .FirstOrDefaultAsync(c => c.Id == id); // Certifique-se que c.Id aqui se refere ao Id do DTO

                if (cidade == null)
                {
                    var errorNotFound = new ErrorDTO
                    {
                        ErrorCode = "RES404CITYNOTFOUND",
                        Message = "Cidade não encontrada.",
                        ErrorDescription = $"Nenhuma cidade foi encontrada com o ID: {id}."
                    };
                    return NotFound(errorNotFound);
                }

                return Ok(cidade);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter cidade por ID: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500GETCITY",
                    Message = "Não foi possível obter os detalhes da cidade.",
                    ErrorDescription = $"Ocorreu um erro inesperado ao buscar a cidade com o ID: {id}"
                }); 
            }
        }

        /// <summary>
        /// Obtém os detalhes de um campus específico pelo seu ID.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de informações detalhadas de um único campus
        /// utilizando seu identificador único.
        /// </remarks>
        /// <param name="id">O ID único do campus a ser pesquisado.</param>
        /// <returns>Os detalhes do campus encontrado.</returns>
        /// <response code="200">Retorna os detalhes do campus com sucesso.</response>
        /// <response code="400">O ID do campus fornecido é inválido.</response>
        /// <response code="404">Nenhum campus foi encontrado com o ID especificado.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("campus/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CampusResponseDTO), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<CampusResponseDTO>> GetCampusById([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDID",
                        Message = "ID do Campus inválido.",
                        ErrorDescription = "O ID fornecido para o campus deve ser um número inteiro positivo."
                        
                    });
                }
                
                var campus = await _context.Campi
                    .Select(c => new CampusResponseDTO(c))
                    .FirstOrDefaultAsync(c => c.IdCampus == id);
                if (campus == null)
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404CAMPUSNOTFOUND", 
                        Message = "Campus não encontrado.", 
                        ErrorDescription = $"Nenhum campus foi encontrado com o ID: {id}."
                    });
                }
                return Ok(campus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter campus por ID: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500GETCAMPUS",
                    Message = "Não foi possível obter os detalhes do campus.",
                    ErrorDescription = $"Ocorreu um erro inesperado ao buscar o campus com o ID: {id}"
                }); 
            }
        }

         /// <summary>
        /// Lista todos os campi associados a uma cidade específica pelo seu ID.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de uma lista de campi localizados em uma cidade,
        /// utilizando o ID da cidade como critério de busca.
        /// </remarks>
        /// <param name="id">O ID único da cidade para a qual se deseja listar os campi.</param>
        /// <returns>Uma lista de objetos <see cref="CampusResponseDTO"/> representando os campi encontrados.</returns>
        /// <response code="200">Retorna a lista de campi da cidade com sucesso.</response>
        /// <response code="400">O ID da cidade fornecido é inválido.</response>
        /// <response code="404">Nenhum campus foi encontrado para o ID de cidade especificado.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("campus-por-cidade/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CampusResponseDTO>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)] 
        [ProducesResponseType(typeof(ErrorDTO), 404)] 
        [ProducesResponseType(typeof(ErrorDTO), 500)] 
        public async Task<ActionResult<IEnumerable<CampusResponseDTO>>> GetCampiByCityId([FromRoute] int id)
        {
            try
            {
                if (id <= 0) 
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDCITYID", 
                        Message = "ID da cidade inválido.",
                        ErrorDescription = "O ID fornecido para a cidade deve ser um número inteiro positivo."
                    });
                }

                var campi = await _context.Campi
                                          .Where(c => c.CdCidade == id)
                                          .Select(c => new CampusResponseDTO(c)) // Mapeia para o DTO
                                          .ToListAsync();

                if (campi == null || !campi.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404CAMPINOTFOUNDFORCITY", 
                        Message = "Nenhum campus encontrado para esta cidade.",
                        ErrorDescription = $"Não foi encontrado nenhum campus para a cidade com ID: {id}."
                    });
                }

                return Ok(campi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter campi por ID da cidade: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTCAMPBYCITY",
                    Message = "Não foi possível listar os campi para esta cidade.",
                    ErrorDescription = $"Ocorreu um erro inesperado ao buscar campi para a cidade com ID: {id}"
                }); 
            }
        }

        /// <summary>
        /// Lista os alunos verificados associados a um campus específico.
        /// </summary>
        /// <remarks>
        /// Este endpoint permite a recuperação de uma lista de usuários (alunos) que pertencem a um determinado campus
        /// e que tiveram seus e-mails verificados.
        /// </remarks>
        /// <param name="campusId">O ID único do campus para o qual se deseja listar os alunos.</param>
        /// <returns>Uma lista de objetos <see cref="SearchedUserInfo"/> representando os alunos encontrados.</returns>
        /// <response code="200">Retorna a lista de alunos do campus com sucesso.</response>
        /// <response code="400">O ID do campus fornecido é inválido.</response>
        /// <response code="404">Nenhum aluno verificado foi encontrado para o ID de campus especificado.</response>
        /// <response code="500">Ocorreu um erro interno no servidor.</response>
        [HttpGet("alunos-por-campus/{campusId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SearchedUserInfo>), 200)]
        [ProducesResponseType(typeof(ErrorDTO), 400)]
        [ProducesResponseType(typeof(ErrorDTO), 404)]
        [ProducesResponseType(typeof(ErrorDTO), 500)]
        public async Task<ActionResult<IEnumerable<SearchedUserInfo>>> GetUserByCampiId([FromRoute] int campusId)
        {
            try
            {
                if (campusId <= 0) 
                {
                    return BadRequest(new ErrorDTO
                    {
                        ErrorCode = "VAL400INVALIDCAMPUSID",
                        Message = "ID do campus inválido.",
                        ErrorDescription = "O ID fornecido para o campus deve ser um número inteiro positivo."
                    });
                }

                var users = await _context.Users
                    .Where(u => u.CdCampus == campusId && u.IsEmailVerified) 
                    .Select(u => new SearchedUserInfo(u)) 
                    .ToListAsync();
                
                if (!users.Any())
                {
                    return NotFound(new ErrorDTO
                    {
                        ErrorCode = "RES404STUDENTSNOTFOUND",
                        Message = "Nenhum aluno verificado encontrado para este campus.",
                        ErrorDescription =
                            $"Não foram encontrados alunos verificados para o campus com ID: {campusId}."
                    });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter alunos por campus: {ex.Message}");
                return StatusCode(500, new ErrorDTO
                {
                    ErrorCode = "GEN500LISTSTUDENTSBYCAMPUS",
                    Message = "Não foi possível listar os alunos para este campus.",
                    ErrorDescription = "Ocorreu um erro inesperado ao buscar os alunos."
                });
            }
        }
    }
}
