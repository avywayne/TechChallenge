namespace TechChallenge.Web.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    public List<TaskItem> Tasks { get; set; } = new();
}
