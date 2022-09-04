public static class JsonExtensions
{
    public static JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        Converters = {
        new JsonStringEnumConverter(),
        new ColorConverter()},
        IncludeFields = true,
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static string JsonSerialize(this object obj, JsonSerializerOptions options = null)
    {
        if (options == null)
            options = DefaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }
    public static T JsonDeserialize<T>(this string str, JsonSerializerOptions options = null)
    {
        if (options == null)
            options = DefaultOptions;
        return JsonSerializer.Deserialize<T>(str, options);
    }
}