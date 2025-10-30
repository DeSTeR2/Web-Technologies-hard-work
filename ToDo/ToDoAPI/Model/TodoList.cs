using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ToDoAPI.Model;

[Serializable]
public class TodoList
{

    public TodoList(string ownerId, string name)
    {
        Id = ObjectId.GenerateNewId().ToString();
        OwnerId = ownerId;
        Name = name;
        NoteIds = new List<string>();
    }

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("OwnerId")] public string? OwnerId { get; set; }

    [BsonElement("Name")] public string? Name { get; set; }

    [BsonElement("NoteIds")] public ICollection<string>? NoteIds { get; set; }

    public void AddNote(string noteId)
    {
        NoteIds ??= new List<string>();
        NoteIds.Add(noteId);
    }

    public void RemoveNote(string noteId)
    {
        NoteIds?.Remove(noteId);
    }
}