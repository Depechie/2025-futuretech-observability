namespace futuretech.Web;

public class TodoApiClient(HttpClient httpClient)
{
    public async Task<TodoItem[]> GetAllTodosAsync(CancellationToken cancellationToken = default)
    {
        List<TodoItem>? todos = null;

        await foreach (var todo in httpClient.GetFromJsonAsAsyncEnumerable<TodoItem>("/todos", cancellationToken))
        {
            if (todo is not null)
            {
                todos ??= [];
                todos.Add(todo);
            }
        }

        return todos?.ToArray() ?? [];
    }

    public async Task<TodoItem?> GetTodoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TodoItem>($"/todos/{id}", cancellationToken);
    }
}

public record TodoItem(int Id, string Title, bool IsCompleted);