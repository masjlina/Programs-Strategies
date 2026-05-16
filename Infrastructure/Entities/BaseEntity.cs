using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}