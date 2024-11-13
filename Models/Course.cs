using System;
using System.ComponentModel.DataAnnotations;

public class Course
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public string CourseDescription { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }

    // Automatically include CreatedAt and UpdatedAt timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
