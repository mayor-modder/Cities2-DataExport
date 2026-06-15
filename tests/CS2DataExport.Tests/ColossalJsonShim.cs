using System.Text.Json;

namespace Colossal.Json;

public static class JSON
{
    public static string Dump<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }
}
