using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ToDoAPI.Model
{
    [Serializable]
    public class TodoNote
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Name")]
        public string? Name { get; set; }

        [BsonElement("Content")]
        public string? Content { get; set; }

        [BsonElement("CreationDate")]
        public DateTime? CreationDate { get; set; }

        [BsonElement("StartDate")]
        public DateTime? StartDate { get; set; }

        [BsonElement("EndDate")]
        public DateTime? EndDate { get; set; }

        [BsonElement("Status")]
        public TodoStatus? Status { get; set; }

        public class Builder
        {
            private readonly TodoNote _note = new();

            public void AddContent(string context) => _note.Content = context;
            public void AddEndDate(DateTime endDate) => _note.EndDate = endDate;
            public void AddName(string name) => _note.Name = name;
            public void AddStatus(TodoStatus status) => _note.Status = status;

            public TodoNote Build()
            {
                _note.Id = ObjectId.GenerateNewId().ToString();
                _note.CreationDate = DateTime.UtcNow;
                _note.Status = _note.Status == TodoStatus.None ? TodoStatus.Waiting : _note.Status;
                return _note;
            }
        }
    }
}