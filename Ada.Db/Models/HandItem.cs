using System.ComponentModel.DataAnnotations;
using Ada.Db.Models.Furniture;

namespace Ada.Db.Models;

public class HandItem
{
    [Key] public int Id { get; init; }
    public string Name { get; init; }
    public ICollection<FurnitureItem> FurnitureItems { get; init; } = [];
}