using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
var clientSocket = server.AcceptSocket(); // wait for client

byte[] PongResponse = Encoding.UTF8.GetBytes("+PONG\r\n");
await clientSocket.SendAsync(PongResponse, SocketFlags.None);

// TcpClient client = server.AcceptTcpClient();
// NetworkStream stream = client.GetStream();
// string message = $"+PONG\\r\\n";
// byte[] responseBytes = Encoding.ASCII.GetBytes(message);
// stream.Write(responseBytes, 0, responseBytes.Length);
