// See https://aka.ms/new-console-template for more information

using ChatServerApp;
using System.Configuration;

internal class Program
{
    private static void Main(string[] args)
    {        
        Console.WriteLine("Start Chat Server!!");
        int port = int.Parse(ConfigurationManager.AppSettings.Get("port"));
        int maxConnections = int.Parse(ConfigurationManager.AppSettings.Get("maxConnections"));

        try
        {
            ChatServer server = new ChatServer(port, maxConnections);
            Thread serverThread = new Thread(() => { server.Start(); });

            serverThread.Start();
            Console.WriteLine("Processing Chat Server...");
            Console.WriteLine("q를 입력하면 서버가 종료됩니다.");

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
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}