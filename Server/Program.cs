using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

public class ChatHistory
{
    public string Sender { get; set; }
    public string Recipient { get; set; }
    public string Message { get; set; }
}


class Program
{
    static ConcurrentBag<TcpClient> clientList = new ConcurrentBag<TcpClient>();
    static List<string> usernameList = new List<string>();
    static ConcurrentDictionary<string, List<string>> chatHistory = new ConcurrentDictionary<string, List<string>>();


    static async Task Main(string[] args)
    {

        int port = 7891;

        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine("Server started. Waiting for connections");

        while (true)
        {
            //Accept a new client
            TcpClient client = await server.AcceptTcpClientAsync();

            //add client to the clientlist
            clientList.Add(client);

            Console.WriteLine("New Client Connected {0}", client.Client.RemoteEndPoint);

            //Create a thread to handle communication with the client
            Thread thread = new Thread(new ParameterizedThreadStart(HandleClientAsync));
            thread.Start(client);

        }

    }



    static async void HandleClientAsync(object? obj)
    {
        //Get the client socket and create a network stream
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();


        //Read the client's message
        while (true)
        {
            //READ
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            string[] portId = client.Client.RemoteEndPoint.ToString().Split(':');
            Console.WriteLine("Received message from {0}:" + message, portId[1]);

            string[] messageParts = message.Split(':');

            if (messageParts[0] == "USERNAME")
            {
                // Add the client to the list
                usernameList.Add(messageParts[1]);

                //Send updated user list
                string updateUser = "USERNAME:" + string.Join(',', usernameList);
                byte[] sendBuffer = Encoding.UTF8.GetBytes(updateUser);

                //broadcast message
                foreach (var c in clientList)
                {
                    await c.GetStream().WriteAsync(sendBuffer,0, sendBuffer.Length);
                }
            }

            else if (messageParts[0] == "DISCONNECT")
            {
                usernameList.Remove(messageParts[1]);
                //Send updated user list
                string updateUser = "USERNAME:" + string.Join(',', usernameList);
                byte[] sendBuffer = Encoding.UTF8.GetBytes(updateUser);

                foreach (var c in clientList)
                {
                    await c.GetStream().WriteAsync(sendBuffer, 0, sendBuffer.Length);
                }

                break;
            }

            else if (messageParts[0] == "MESSAGE")
            {
                //Send updated user list
                string forwardMessage = message;
                byte[] sendBuffer = Encoding.UTF8.GetBytes(forwardMessage);

                //broadcast message
                foreach (var c in clientList)
                {
                    await c.GetStream().WriteAsync(sendBuffer, 0, sendBuffer.Length);
                }
            }
        }
        

    }
}


