using System.ComponentModel.DataAnnotations;

public class Admin
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string? Username { get; set; }

    [Required]
    public string? Password { get; set; }
}
