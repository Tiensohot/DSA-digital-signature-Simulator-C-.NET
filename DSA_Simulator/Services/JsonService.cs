using System.IO;
using System.Text.Json;


namespace DSA_DigitalSignature.Services
{
    public static class JsonService
    {
        public static void Save<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        public static T Load<T>(string path) where T : class
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }
}

