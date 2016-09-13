using System;

namespace HTK__GPI_Listener
{
    class Program
    {
        public static string ip = "";
        public static int power = 0;
        public static string server = "";
        public static string ipDB = "";
        static void Main(string[] args)
        {
            Console.Title = "Antena de Emergencia " + "192.168.1.232";
            if (args.Length > 0)
            {
                GetData(args);
            }
            else
            {
                Console.Write("Escriba la IP de la antena: ");
                ip = Console.ReadLine();
                Console.Write("Escriba a potencia de la antena: ");
                power = Convert.ToInt32(Console.ReadLine());
                Console.Write("Escriba la IP del servidor: ");
                server = Console.ReadLine();
                RestPublisher.URL = server;
            }

            Console.Title = "Antena de Emergencia " + ip;

            Reader.ConnectAndWatch(ip, power, server);
            while (true)
            {
            }
        }

        private static void GetData(string[] args)
        {
            if (args.Length < 6)
            {
                System.Environment.Exit(0);
            }
            else
            {
                try
                {
                    ip = args[0];
                    power = Convert.ToInt32(args[1]);
                    int RssiMin = Convert.ToInt32(args[2]);
                    server = args[3];
                    RestPublisher.WaitTime = Convert.ToInt32(args[4]);
                    //URLGPIO = args[4];
                    ipDB = args[5];

                    if (!server.Contains("http"))
                    {
                        server = "http://" + server + "/api/";
                    }

                    if (power < 0 || power > 300) System.Environment.Exit(0);
                    if (RssiMin < 0 || RssiMin > 100) System.Environment.Exit(0);
                }
                catch (Exception)
                {
                    System.Environment.Exit(0);
                }
            }
        }
    }
}
