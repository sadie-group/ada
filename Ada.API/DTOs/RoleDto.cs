namespace Ada.API.DTOs;

public record RoleDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ICollection<PermissionDto> Permissions { get; init; } = [];
}