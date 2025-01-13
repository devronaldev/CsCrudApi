using CsCrudApi.Models;
using CsCrudApi.Models.PostRelated;
using CsCrudApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CommentController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        public async Task<ActionResult<object>> CreateCommentary([FromHeader] string token, [FromBody] Commentary commentary)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(commentary.PostGUID))
            {
                return BadRequest(new
                {
                    Message = "Verifique se todos os campos foram preenchidos."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Erro na serialização do comentário."
                });
            }

            try
            {
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        message = "Token Inválido ou usuário não encontrado."
                    });
                }

                // Verifica se o Post existe
                var post = await _context.Posts.FirstOrDefaultAsync(p => p.Guid == commentary.PostGUID);
                if (post == null)
                {
                    return NotFound(new
                    {
                        Message = "Post não encontrado."
                    });
                }

                commentary.CreatedAt = DateTime.UtcNow;
                commentary.LastUpdatedAt = DateTime.UtcNow;
                commentary.UserId = user.UserId;
                _context.Commentaries.Add(commentary);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro inesperado: {ex.Message}");
            }

            return Ok(commentary);
        }

        [AllowAnonymous]
        [HttpGet("{guid}")]
        public async Task<ActionResult<object>> GetCommentByGUID([FromRoute] string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return BadRequest(new
                {
                    Message = "Por favor, informe um GUID."
                });
            }

            try
            {
                var comments = await _context.Commentaries.Where(c => c.PostGUID == guid).ToListAsync();

                if (comments == null)
                {
                    return NotFound(new
                    {
                        Message = "Não foram encontrados comentários."
                    });
                }

                List<object> result = new List<object>();
                foreach (var comment in comments)
                {
                    var user = await _context.Users
                        .Select(u => new
                        {
                            u.UserId,
                            u.NmSocial,
                            u.ProfilePictureUrl
                        })
                        .FirstOrDefaultAsync(u => u.UserId == comment.UserId);
                    result.Add(new
                    {
                        user,
                        comment
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<ActionResult<object>> UpdateCommentary([FromHeader] string token, [FromBody] Commentary updatedCommentary)
        {
            if (string.IsNullOrEmpty(updatedCommentary.PostGUID))
            {
                return BadRequest(new
                {
                    Message = "Verifique se todos os campos foram preenchidos."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Erro na serialização do comentário."
                });
            }

            if (string.IsNullOrEmpty(updatedCommentary.Text) || updatedCommentary.Text.Length < 3)
            {
                return BadRequest(new
                {
                    Message = "O comentário precisa ter texto igual ou superior a 3 letras."
                });
            }

            try
            {
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        Message = "Token Inválido ou usuário não encontrado."
                    });
                }

                if (!await _context.Posts.AnyAsync(p => p.Guid == updatedCommentary.PostGUID))
                {
                    return NotFound(new
                    {
                        Message = "Post não encontrado."
                    });
                }

                var savedCommentary = await _context.Commentaries.FirstOrDefaultAsync(sc => sc.Id == updatedCommentary.Id);

                if (savedCommentary == null)
                {
                    return NotFound(new
                    {
                        Message = "Comentário não encontrado."
                    });
                }

                if (savedCommentary.UserId != user.UserId)
                {
                    return Forbid();
                }

                savedCommentary.Text = updatedCommentary.Text;
                savedCommentary.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(savedCommentary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<ActionResult<dynamic>> DeleteCommentary([FromHeader]string token, int commentaryId)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    Message = "O token não pode ser vazio."
                });
            }

            if(commentaryId == 0)
            {
                return BadRequest(new
                {
                    Message = "O id precisa estar preenchido."
                });
            }

            try
            {
                var user = await TokenServices.GetTokenUserAsync(TokenServices.ValidateJwtToken(token), _context);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        Message = "Token Inválido ou usuário não encontrado."
                    });
                }

                var commentary = await _context.Commentaries.FirstOrDefaultAsync(c => c.Id == commentaryId);
                if (commentary == null)
                {
                    return NotFound(new
                    {
                        Message = "Comentário não encontrado."
                    });
                }

                if (commentary.UserId != user.UserId)
                {
                    return Forbid();
                }

                _context.Commentaries.Remove(commentary);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }
    }
}