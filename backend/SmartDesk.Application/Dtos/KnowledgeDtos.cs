using System.ComponentModel.DataAnnotations;
using SmartDesk.Application.Common;

namespace SmartDesk.Application.Dtos;

public sealed class UpsertArticleRequest
{
    [Required, MinLength(5), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(20), MaxLength(20000)]
    public string Body { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [MaxLength(10)]
    public string[] Tags { get; set; } = [];

    public bool IsPublished { get; set; } = true;
}

public sealed class ArticleQuery : PagedQuery
{
    [MaxLength(200)]
    public string? Search { get; set; }

    public int? CategoryId { get; set; }
    public bool? IsPublished { get; set; }
}

public sealed record ArticleListItemDto(
    int Id,
    string Title,
    string Excerpt,
    int CategoryId,
    string CategoryName,
    string[] Tags,
    bool IsPublished,
    int ViewCount,
    DateTime UpdatedAt);

public sealed record ArticleDetailDto(
    int Id,
    string Title,
    string Body,
    int CategoryId,
    string CategoryName,
    string[] Tags,
    bool IsPublished,
    int ViewCount,
    string? AuthorName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>An article scored against a ticket by the deterministic relevance function.</summary>
public sealed record RelevantArticleDto(
    int Id,
    string Title,
    string Excerpt,
    string CategoryName,
    double RelevanceScore,
    string MatchReason);

public sealed class LinkArticleRequest
{
    [Range(1, int.MaxValue)]
    public int ArticleId { get; set; }
}
