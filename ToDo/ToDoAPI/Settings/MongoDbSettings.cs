namespace ToDoAPI.Settings
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "ToDoDb";
        public string TodoListsCollection { get; set; } = "ToDoLists";
        public string TodoNotesCollection { get; set; } = "TodoNotes";
    }
}