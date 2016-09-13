using HTKCSL;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Antenna_Tester
{
    class Program
    {
        public static CS203 Antenna;
        public static string IP { get; set; }
        public static int Power { get; set; }
        public static int RssiMin { get; set; }
        public static string URLTags { get; set; }
        public static string URLGPIO { get; set; }
        public enum PassType
        {
            Entrada, Salida, None
        }
        public class TagState
        {
            public PassType Pass { get; set; }
            public int EraseTime { get; set; }
            public TagState()
            {
                Pass = PassType.None;
                EraseTime = waitTime;
            }
            public TagState(PassType pass)
            {
                Pass = pass;
                EraseTime = waitTime;
            }
            public TagState(int eraseTime)
            {
                Pass = PassType.None;
                EraseTime = eraseTime;
            }
            public TagState(PassType pass, int eraseTime)
            {
                Pass = pass;
                EraseTime = eraseTime;
            }
            public void CountDown()
            {
                if (EraseTime > 0)
                {
                    EraseTime--;
                }
                else
                {
                    EraseTime = 0;
                }
            }

        }
        public static int waitTime = 10;

        static bool simplePass = false;
        public static ConcurrentDictionary<string, TagState> HoldTags = new ConcurrentDictionary<string, TagState>();
        public static System.Timers.Timer Wait = new System.Timers.Timer(1000);
        public static ProcessState LastState = ProcessState.Inactivo;
        static void Main(string[] args)
        {
            IP = "192.168.25.203";
            Power = 150;
            RssiMin = 60;
            URLTags = "http://192.168.1.219/api/";
            URLGPIO = "http://192.168.1.219/api/";
            waitTime = 10;
            try
            {
                Console.Title = "Antena Simple: " + IP;
                Wait.Elapsed += Wait_Elapsed;
                Wait.Start();
                Antenna = new CS203(IP, Power);
                Task<bool> TaskConnect = new Task<bool>(() => Antenna.Connect(true, true));
                Console.WriteLine("\n\n\nIniciando antena con los siguientes datos:");
                Console.WriteLine("IP:{0} Power:{1} RssiMin:{2} URLTags:{3} URLGPIO:{4} WaitTime:{5} SimplePass:{6}", IP, Power, RssiMin, URLTags, URLGPIO, waitTime.ToString(), simplePass.ToString());
                Console.Write("\nConectando...");
                TaskConnect.Start();
                while (!TaskConnect.IsCompleted)
                {
                    Console.Write(".");
                    Thread.Sleep(1000);
                }
                Antenna.Connected = TaskConnect.Result;
                if (Antenna.Connected)
                {
                    Console.Write("Conexión exitosa\n");
                    Antenna.Sensors = true;
                    Antenna.EraseTime = waitTime > 1 ? waitTime - 1 : 1;
                    Antenna.SimpleEntence = simplePass;
                    Antenna.Entrance += Antenna_Entrance;
                    Antenna.Exit += Antenna_Exit;
                    Antenna.PassStatusChange += Antenna_PassStatusChange;
                    while (Antenna.Connected)
                    {
                        Thread.Sleep(100);
                    }
                    Antenna.Connected = false;
                }
                Console.WriteLine("Desconectando..");
                System.Environment.Exit(0);
            }
            catch (Exception)
            {
                Console.WriteLine("La conexión ha fallado.");
                Thread.Sleep(1000);
                System.Environment.Exit(0);
            }
            finally
            {
                Antenna.Entrance -= Antenna_Entrance;
                Antenna.Exit -= Antenna_Exit;
                Antenna.PassStatusChange -= Antenna_PassStatusChange;
                Antenna.Stop();
            }
        }

        private static void Antenna_PassStatusChange(ProcessState state)
        {
            Console.WriteLine("State:\t\t" + state.ToString());
        }

        private static void Antenna_Exit(TagData[] tags)
        {
            foreach (TagData tag in tags)
            {
                Console.WriteLine("Exit:\t\t" + tag.EPC);
            }
        }

        private static void Antenna_Entrance(TagData[] tags)
        {
            foreach (TagData tag in tags)
            {
                Console.WriteLine("Entrance:\t" + tag.EPC);
            }
        }

        private static void Wait_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var item in HoldTags)
            {
                item.Value.CountDown();
            }
        }
    }
}
