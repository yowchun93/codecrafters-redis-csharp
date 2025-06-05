using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();


// var clientSocket = await server.AcceptSocketAsync(); // wait for client, AcceptSocket is blocking
// // HandleConnectionAsync(clientSocket);
// _ = Task.Run(() => HandleConnectionAsync(clientSocket));

// use Tasks to allow server to accept connections asynchronously
// each Task runs independently
while (true)
{
    var clientSocket = await server.AcceptSocketAsync(); // Accept new client asynchronously
    Console.WriteLine("New client connected");

    _ = Task.Run(() => HandleConnectionAsync(clientSocket));
}

static async Task HandleConnectionAsync(Socket clientSocket)
{
    var buffer = new byte[1024];
    var inputBuilder = new StringBuilder();

    try
    {
        int bytesRead;

        while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            inputBuilder.Append(message);

            if (!inputBuilder.ToString().EndsWith("\r\n"))
                continue;

            var rawInput = inputBuilder.ToString();
            var parsed = RedisParser.Parse(rawInput);

            if (parsed == null)
            {
                inputBuilder.Clear();
                continue;
            }

            var (command, args) = parsed.Value;

            if (command == "PING")
            {
                // Respond with +PONG\r\n
                var pongResponse = Encoding.UTF8.GetBytes("+PONG\r\n");
                await clientSocket.SendAsync(pongResponse, SocketFlags.None);
            }
            else if (command == "ECHO" && args.Count == 1)
            {
                string response = $"+{args[0]}\r\n";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await clientSocket.SendAsync(responseBytes, SocketFlags.None);
            }
            else
            {
                string error = "-ERR unknown command\r\n";
                var errorBytes = Encoding.UTF8.GetBytes(error);
                await clientSocket.SendAsync(errorBytes, SocketFlags.None);
            }

            inputBuilder.Clear(); // Clear buffer after processing
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
        clientSocket.Close();
        Console.WriteLine("Client disconnected");
    }
}

static class RedisParser
{
    public static (string commandName, List<string> arguments)? Parse(string input)
    {
        string[] lines = input.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 3 || !lines[0].StartsWith("*"))
            return null;

        if (!int.TryParse(lines[0][1..], out int argc))
            return null;

        // Build arguments
        var args = new List<string>();

        for (int i = 0, index = 1; i < argc && index < lines.Length; i++, index += 2)
        {
            if (!lines[index].StartsWith("$")) return null;
            args.Add(lines[index + 1]);
        }

        if (args.Count == 0)
            return null;

        var commandName = args[0].ToUpperInvariant();
        var arguments = args.Count > 1 ? args.GetRange(1, args.Count - 1) : new List<string>();

        // [Echo, hey]
        return (commandName, arguments);
    }
}

