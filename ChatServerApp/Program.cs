// See https://aka.ms/new-console-template for more information

using ChatServerApp;

try
{
    Console.WriteLine("Start Chat Server!!");
    int port = 8888;
    int maxConnections = 1000;
    ChatServer server = new ChatServer(port, maxConnections);

    Thread serverThread = new Thread(() => { server.Start(); });

    serverThread.Start();
    Console.WriteLine("Processing Chat Server...");

    while (true)
    {
        string input = Console.ReadLine();
        if (input?.ToLower() == "q")
        {
            Console.WriteLine("Terminate Chat Server");
            // 서버 종료 로직 추가
            server.Stop();
            serverThread.Join();
            break;
        }
    }
}
catch(Exception ex)
{ 
    Console.WriteLine(ex.ToString()); 
}
