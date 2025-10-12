namespace ToDoAPI.Model;

[Serializable]
public class TodoNote
{
    public string? Id { get; set; }
    public string? Content { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TodoStatus? Status { get; set; }

    public class Builder
    {
        private readonly TodoNote _note = new();
        
        public void AddContent(string context) => _note.Content = context;
        public void AddEndDate(DateTime endDate) => _note.EndDate = endDate;

        public TodoNote Build()
        {
            _note.Id = Guid.NewGuid().ToString();
            _note.CreationDate = DateTime.Now;
            _note.Status = TodoStatus.Waiting;
            return _note;
        }
    }
}