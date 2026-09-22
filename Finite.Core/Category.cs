using System.ComponentModel.DataAnnotations;

namespace Finite.Core;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; }

    public int? ParentId { get; set; }

    public Category? Parent { get; set; }

    [MaxLength(7)]
    public string Color { get; set; } = "#6B7280";

    [MaxLength(255)]
    public string? Icon { get; set; }

    /// <summary>System categories are seeded and cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string? Description { get; set; }
}
