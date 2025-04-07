using DotNetEnv;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    // These will be loaded from the .env file
    private static string? odooUrl;
    private static string? db;
    private static string? username;
    private static string? password;


    static async Task Main()
    {
        try
        {
            // Load the .env file
            Env.Load();

            odooUrl = Env.GetString("ODOO_URL") ?? throw new InvalidOperationException("ODOO_URL is missing");
            db = Env.GetString("ODOO_DB") ?? throw new InvalidOperationException("ODOO_DB is missing");
            username = Env.GetString("ODOO_USER") ?? throw new InvalidOperationException("ODOO_USER is missing");
            password = Env.GetString("ODOO_PASSWORD") ?? throw new InvalidOperationException("ODOO_PASSWORD is missing");


            Console.WriteLine("Conectando a Odoo...");

            int uid = await AuthenticateAsync();
            if (uid <= 0)
            {
                Console.WriteLine("Autenticación fallida.");
                return;
            }

            Console.WriteLine($"Autenticación exitosa. UID: {uid}");

            var productIds = await SearchProductsAsync(uid);
            if (productIds?.Length > 0)
            {
                await ReadProductsAsync(uid, productIds);
            }
            else
            {
                Console.WriteLine("No se encontraron productos.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excepción: {ex.Message}");
        }
    }

    private static async Task<int> AuthenticateAsync()
    {
        var request = new JsonRpcRequest
        {
            Method = "call",
            Params = new JsonRpcParams
            {
                Service = "common",
                Method = "login",
                Args = new object[] { db!, username!, password! }
            },
            Id = 1
        };

        Console.WriteLine("Autenticando con Odoo...");

        var response = await SendJsonRpcRequestAsync(request);
        if (response?.Result is JsonElement result && result.ValueKind == JsonValueKind.Number)
        {
            return result.GetInt32();
        }

        PrintError(response?.Error);
        return 0;
    }

    private static async Task<int[]?> SearchProductsAsync(int uid)
    {
        var request = new JsonRpcRequest
        {
            Method = "call",
            Params = new JsonRpcParams
            {
                Service = "object",
                Method = "execute_kw",
                Args = new object[] { db!, uid, password!, "product.product", "search", new object[] { new object[] { } } }
            },
            Id = 2
        };

        var response = await SendJsonRpcRequestAsync(request);
        if (response?.Result is JsonElement result && result.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<int[]>(result);
        }

        PrintError(response?.Error);
        return null;
    }

    private static async Task ReadProductsAsync(int uid, int[] productIds)
    {
        var request = new JsonRpcRequest
        {
            Method = "call",
            Params = new JsonRpcParams
            {
                Service = "object",
                Method = "execute_kw",
                Args = new object[] { db!, uid, password!, "product.product", "read", new object[] { productIds, new[] { "name", "barcode" } } }
            },
            Id = 3
        };

        var response = await SendJsonRpcRequestAsync(request);
        if (response?.Result is JsonElement result && result.ValueKind == JsonValueKind.Array)
        {
            var products = JsonSerializer.Deserialize<Product[]>(result);
            foreach (var product in products!)
            {
                Console.WriteLine($"Producto: {product.Name} - Barcode: {product.Barcode}");
            }
        }
        else
        {
            PrintError(response?.Error);
        }
    }

    private static async Task<JsonRpcResponse?> SendJsonRpcRequestAsync(JsonRpcRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync($"{odooUrl}/jsonrpc", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonRpcResponse>(responseBody);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error de conexión: {ex.Message}");
            return null;
        }
    }

    private static void PrintError(JsonElement? error)
    {
        if (error.HasValue)
        {
            Console.WriteLine("Error en la respuesta de Odoo:");
            Console.WriteLine(error.Value.ToString());
        }
    }
}

// Request and Response classes
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string Jsonrpc { get; set; } = "2.0";
    [JsonPropertyName("method")] public string Method { get; set; } = "call";
    [JsonPropertyName("params")] public JsonRpcParams Params { get; set; } = new();
    [JsonPropertyName("id")] public int Id { get; set; }
}

public class JsonRpcParams
{
    [JsonPropertyName("service")] public string Service { get; set; } = "";
    [JsonPropertyName("method")] public string Method { get; set; } = "";
    [JsonPropertyName("args")] public object[] Args { get; set; } = Array.Empty<object>();
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string? Jsonrpc { get; set; }
    [JsonPropertyName("result")] public JsonElement? Result { get; set; }
    [JsonPropertyName("error")] public JsonElement? Error { get; set; }
    [JsonPropertyName("id")] public int Id { get; set; }
}

public class Product
{
    [JsonPropertyName("name")]
    public JsonElement NameElement { get; set; }

    [JsonPropertyName("barcode")]
    public JsonElement BarcodeElement { get; set; }

    public string Name => NameElement.ValueKind == JsonValueKind.String ? NameElement.GetString()! : $"(Tipo inesperado: {NameElement.ValueKind})";

    public string Barcode => BarcodeElement.ValueKind == JsonValueKind.String ? BarcodeElement.GetString()! : $"(Tipo inesperado: {BarcodeElement.ValueKind})";
}
