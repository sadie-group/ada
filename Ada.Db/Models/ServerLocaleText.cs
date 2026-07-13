using System.ComponentModel.DataAnnotations;

namespace Ada.Db.Models;

public class ServerLocaleText
{
    public int Id { get; set; }
    [MaxLength(120)] public required string Key { get; set; }
    [MaxLength(300)] public required string Text { get; set; }
}