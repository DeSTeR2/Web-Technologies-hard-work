using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ToDoAPI.Model;

namespace ToDoAPI.Settings;

public class MongoDbService
{
    private readonly IMongoCollection<TodoList> _lists;
    private readonly IMongoCollection<TodoNote> _notes;

    public MongoDbService(IOptions<MongoDbSettings> options)
    {
        var s = options.Value;
        var client = new MongoClient(s.ConnectionString);
        var db = client.GetDatabase(s.DatabaseName);

        _lists = db.GetCollection<TodoList>(s.TodoListsCollection);
        _notes = db.GetCollection<TodoNote>(s.TodoNotesCollection);
    }

    public async Task<List<TodoList>> GetAllListsAsync()
    {
        return await (await _lists.FindAsync(_ => true)).ToListAsync();
    }

    public async Task<List<TodoList>> GetAllListsByUserIdAsync(string? userId)
    {
        return await (await _lists.FindAsync(list => list.OwnerId == userId)).ToListAsync();
    }

    public async Task<TodoList> FindListAsync(string id)
    {
        var filter = Builders<TodoList>.Filter.Eq(l => l.Id, id);
        var list = await _lists.Find(filter).FirstOrDefaultAsync();
        return list ?? throw new InvalidOperationException($"List {id} not found");
    }

    public async Task<TodoList> CreateListAsync(string ownerId, string name)
    {
        var list = new TodoList(ownerId, name);
        await _lists.InsertOneAsync(list);
        return list;
    }

    public async Task<TodoList> DeleteListAsync(string id)
    {
        var filter = Builders<TodoList>.Filter.Eq(l => l.Id, id);
        var result = await _lists.FindOneAndDeleteAsync(filter);
        return result ?? throw new InvalidOperationException($"List {id} not found");
    }

    public async Task<TodoList> AddNoteToListAsync(string listId, string noteId)
    {
        var filter = Builders<TodoList>.Filter.Eq(l => l.Id, listId);
        var update = Builders<TodoList>.Update.AddToSet(l => l.NoteIds, noteId);
        var options = new FindOneAndUpdateOptions<TodoList> { ReturnDocument = ReturnDocument.After };
        var updated = await _lists.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"List {listId} not found");
    }

    public async Task<TodoList> RemoveNoteFromListAsync(string listId, string noteId)
    {
        var filter = Builders<TodoList>.Filter.Eq(l => l.Id, listId);
        var update = Builders<TodoList>.Update.Pull(l => l.NoteIds, noteId);
        var options = new FindOneAndUpdateOptions<TodoList> { ReturnDocument = ReturnDocument.After };
        var updated = await _lists.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"List {listId} not found");
    }

    public async Task<TodoNote> FindNoteAsync(string id)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var note = await _notes.Find(filter).FirstOrDefaultAsync();
        return note ?? throw new InvalidOperationException($"Note {id} not found");
    }

    public async Task<TodoNote> CreateNoteAsync(TodoNote note)
    {
        if (string.IsNullOrWhiteSpace(note.Id))
            note.Id = ObjectId.GenerateNewId().ToString();

        note.CreationDate ??= DateTime.UtcNow;

        await _notes.InsertOneAsync(note);
        return note;
    }


    public async Task<TodoNote> UpdateNoteAsync(TodoNote note)
    {
        if (string.IsNullOrWhiteSpace(note.Id))
            throw new ArgumentException("Note.Id must be set for update");

        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, note.Id);
        var opts = new ReplaceOptions { IsUpsert = false };
        var result = await _notes.ReplaceOneAsync(filter, note, opts);

        if (result == null || result.MatchedCount == 0)
            throw new InvalidOperationException($"Note {note.Id} not found");

        return await FindNoteAsync(note.Id);
    }


    public async Task<TodoNote> DeleteNoteAsync(string id)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var deleted = await _notes.FindOneAndDeleteAsync(filter);
        return deleted ?? throw new InvalidOperationException($"Note {id} not found");
    }

    public async Task<TodoNote> PatchNoteContentAsync(string id, string content)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var update = Builders<TodoNote>.Update.Set(n => n.Content, content);
        var options = new FindOneAndUpdateOptions<TodoNote> { ReturnDocument = ReturnDocument.After };
        var updated = await _notes.FindOneAndUpdateAsync(filter, update, options);

        if (updated == null)
            throw new InvalidOperationException($"Note {id} not found (update returned null).");

        return updated;
    }

    public async Task<TodoNote> PatchNoteStartDateAsync(string id, DateTime startDate)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var update = Builders<TodoNote>.Update.Set(n => n.StartDate, startDate);
        var options = new FindOneAndUpdateOptions<TodoNote> { ReturnDocument = ReturnDocument.After };
        var updated = await _notes.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"Note {id} not found");
    }

    public async Task<TodoNote> PatchNoteEndDateAsync(string id, DateTime endDate)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var update = Builders<TodoNote>.Update.Set(n => n.EndDate, endDate);
        var options = new FindOneAndUpdateOptions<TodoNote> { ReturnDocument = ReturnDocument.After };
        var updated = await _notes.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"Note {id} not found");
    }

    public async Task<TodoNote> PatchNoteStatusAsync(string id, TodoStatus status)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var update = Builders<TodoNote>.Update.Set(n => n.Status, status);
        var options = new FindOneAndUpdateOptions<TodoNote> { ReturnDocument = ReturnDocument.After };
        var updated = await _notes.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"Note {id} not found");
    }

    public async Task<List<string>> GetNoteIdsForListAsync(string listId)
    {
        var list = await FindListAsync(listId);
        return list.NoteIds is null ? new List<string>() : new List<string>(list.NoteIds);
    }

    public async Task<int> RemoveDuplicateNotesAsync()
    {
        var all = await (await _notes.FindAsync(_ => true)).ToListAsync();
        var groups = all.GroupBy(n => new { n.Content, n.CreationDate })
            .Where(g => g.Count() > 1);

        var removed = 0;
        foreach (var g in groups)
        {
            var keep = g.OrderByDescending(n => n.CreationDate).First();
            var duplicates = g.Where(n => n.Id != keep.Id);
            foreach (var dup in duplicates)
            {
                await _notes.DeleteOneAsync(Builders<TodoNote>.Filter.Eq(x => x.Id, dup.Id));
                removed++;
            }
        }

        return removed;
    }

    public async Task<TodoNote> PatchNoteTitleAsync(string id, string title)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, id);
        var update = Builders<TodoNote>.Update.Set(n => n.Name, title);
        var options = new FindOneAndUpdateOptions<TodoNote> { ReturnDocument = ReturnDocument.After };
        var updated = await _notes.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"Note {id} not found");
    }

    public async Task<TodoNote> UpdateNoteImagesAsync(string noteId, List<string> imageUrls)
    {
        var filter = Builders<TodoNote>.Filter.Eq(n => n.Id, noteId);
        var update = Builders<TodoNote>.Update.Set(n => n.ImageUrls, imageUrls);
        var options = new FindOneAndUpdateOptions<TodoNote> { ReturnDocument = ReturnDocument.After };

        var updated = await _notes.FindOneAndUpdateAsync(filter, update, options);
        return updated ?? throw new InvalidOperationException($"Note {noteId} not found");
    }
}