namespace Application.Dtos;

/// <summary>
/// Base DTO with nullable entity identifier.
/// </summary>
public abstract class BaseDto
{
    /// <summary>
    /// Entity identifier.
    /// </summary>
    public Guid? Id { get; set; }
}
