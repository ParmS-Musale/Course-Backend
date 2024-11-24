using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    public int Id { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; } = "user";

    // Relationship to Course
    public ICollection<Course> PurchasedCourses { get; set; }
}
