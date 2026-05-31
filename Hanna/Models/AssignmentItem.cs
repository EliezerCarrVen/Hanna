namespace Hanna.Models;

internal sealed class AssignmentItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;
    public string Subject { get; set; } = "Materia";
    public string Title { get; set; } = "Tarea";
    public string Source { get; set; } = "Manual";
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DueAt { get; set; } = DateTime.Now.AddDays(1);
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<int> NotifiedHours { get; set; } = new();
    public bool NotebookCreated { get; set; }
    public bool Completed { get; set; }
}
