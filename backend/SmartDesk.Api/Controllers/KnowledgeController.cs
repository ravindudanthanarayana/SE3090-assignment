using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;

namespace SmartDesk.Api.Controllers;

/// <summary>Component C - Knowledge Base and Solutions (Student 3).</summary>
[ApiController]
[Route("api/knowledge-articles")]
[Authorize]
[Produces("application/json")]
public sealed class KnowledgeController(KnowledgeService knowledge) : ControllerBase
{
    /// <summary>Lists articles with search, category filter, sorting and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ArticleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ArticleListItemDto>>> List([FromQuery] ArticleQuery query, CancellationToken ct)
        => Ok(await knowledge.QueryAsync(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ArticleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await knowledge.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(ArticleDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleDetailDto>> Create(UpsertArticleRequest request, CancellationToken ct)
    {
        var article = await knowledge.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = article.Id }, article);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(ArticleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDetailDto>> Update(int id, UpsertArticleRequest request, CancellationToken ct)
        => Ok(await knowledge.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await knowledge.DeleteAsync(id, ct);
        return NoContent();
    }
}
