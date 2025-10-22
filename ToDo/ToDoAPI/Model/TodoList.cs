using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ToDoAPI.Model
{
    [Serializable]
    public class TodoList
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } = null!;

        [BsonElement("OwnerId")]
        public string? OwnerId { get; set; }

        [BsonElement("Name")]
        public string? Name { get; set; }

        [BsonElement("NoteIds")]
        public ICollection<string>? NoteIds { get; set; }

        // Keep constructor shape similar: ownerId + name
        public TodoList(string ownerId, string name)
        {
            Id = ObjectId.GenerateNewId().ToString();
            OwnerId = ownerId;
            Name = name;
            NoteIds = new List<string>();
        }

        public void AddNote(string noteId)
        {
            NoteIds ??= new List<string>();
            NoteIds.Add(noteId);
        }

        public void RemoveNote(string noteId) => NoteIds?.Remove(noteId);
    }
}