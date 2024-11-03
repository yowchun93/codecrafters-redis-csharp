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
    var pongResponse = "+PONG\r\n"u8.ToArray();
    
    try
    {
        int bytesRead;
        
        while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
        {
            if (bytesRead == 0)
            {
                break; // Client has disconnected
            }
        
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            inputBuilder.Append(message);
        
            var lines = inputBuilder.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var pingCount = lines.Count(line => line.Trim() == "PING");
        
            if (inputBuilder.Length > 0 && inputBuilder[^1] == '\n')
            {
                for (var i = 0; i < pingCount; i++)
                {
                    await clientSocket.SendAsync(new ArraySegment<byte>(pongResponse), SocketFlags.None);
                }
                
                inputBuilder.Clear();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
        clientSocket.Close(); // Ensure the socket is closed when done
        Console.WriteLine("Client disconnected");
    }
}

