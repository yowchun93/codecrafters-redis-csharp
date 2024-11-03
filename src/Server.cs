using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
var clientSocket = server.AcceptSocket(); // wait for client, AcceptSocket is blocking 

var buffer = new byte[1024];
var inputBuilder = new StringBuilder();
int bytesRead;
var pongResponse = "+PONG\r\n"u8.ToArray();

while ((bytesRead = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
{
    if (bytesRead == 0)
    {
        break;
    }
    
    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    inputBuilder.Append(message);
    
    var lines = inputBuilder.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    var pingCount = lines.Aggregate(0, (count, line) => count + (line.Trim() == "PING" ? 1 : 0));
    
    if (inputBuilder.Length > 0 && inputBuilder[^1] == '\n')
    {
        for (var i = 0; i < pingCount; i++)
        {
            await clientSocket.SendAsync(new ArraySegment<byte>(pongResponse), SocketFlags.None);
        }
        
        inputBuilder.Clear();
    }
}
