using HTKCSL;
using HTKRestClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MDW_AntenaDoble
{
    class Program
    {
        public static CS203 Antenna1;
        public static CS203 Antenna2;
        public static string IP1 { get; set; }
        public static string IP2 { get; set; }
        public static int Power1 { get; set; }
        public static int Power2 { get; set; }
        public static int RssiMin1 { get; set; }
        public static int RssiMin2 { get; set; }
        public static string URLTags { get; set; }
        public static string URLGPIO { get; set; }
        public static bool Conectado1 { get; set; }
        public static bool Conectado2 { get; set; }
        public static string TagServiceURL { get; set; }
        public static int Wait { get; set; }
        public static string IPDB { get; set; }

        static CancellationTokenSource tokenSource2;
        static CancellationToken ct;
        public static ConcurrentDictionary<string, int> ToExit = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> OnReading = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> Blocked = new ConcurrentDictionary<string, int>();
        public static System.Timers.Timer ExitTimer = new System.Timers.Timer(60000);
        public static System.Timers.Timer BlockedTimer = new System.Timers.Timer(1000);
        public static System.Timers.Timer flashTimer = new System.Timers.Timer(200);

        static void Main(string[] args)
        {
            Wait = 2;
            GetData(args);
            Console.SetBufferSize(160, 300);
            Console.SetWindowPosition(0, 5);
            Console.SetWindowSize(160, 10);
            Antenna1 = new CS203(IP1, Power1);
            Antenna2 = new CS203(IP2, Power2);
            Console.WriteLine("Antena Doble " + IP1 + "-" + IP2);
            Console.Title = "Antena Doble " + IP1 + "-" + IP2;
            Thread.Sleep(1000);
            Conectado1 = true;
            tokenSource2 = new CancellationTokenSource();
            ct = tokenSource2.Token;
            Task.Factory.StartNew(() =>
            {
                ct.ThrowIfCancellationRequested();
                LeerDoble();
            }, tokenSource2.Token);
            while (true)
            {

            }
        }

        private static void GetData(string[] args)
        {

            if (args.Length < 9)
            {
                Console.WriteLine("Escriba los datos de la primera antena:");
                Console.Write("IP: ");
                IP1 = Console.ReadLine();
                Console.Write("Potencia: ");
                Power1 = Convert.ToInt32(Console.ReadLine());
                Console.Write("Rssi mínimo: ");
                RssiMin1 = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Escriba los datos de la segunda antena:");
                Console.Write("IP: ");
                IP2 = Console.ReadLine();
                Console.Write("Potencia: ");
                Power2 = Convert.ToInt32(Console.ReadLine());
                Console.Write("Rssi mínimo: ");
                RssiMin2 = Convert.ToInt32(Console.ReadLine());

                Console.Write("Escriba la IP del servidor: ");
                string ip = Console.ReadLine();
                string url = "";
                if (ip.Contains("http"))
                {
                    url = ip;
                }
                else
                {
                    url = "http://" + ip + "/api/";
                }
                Console.Write("Escriba la IP de la base de datos: ");
                IPDB = Console.ReadLine();
                URLTags = url;
                TagServiceURL = url;
                URLGPIO = url;

            }
            else
            {
                try
                {
                    IP1 = args[0];
                    Power1 = Convert.ToInt32(args[1]);
                    RssiMin1 = Convert.ToInt32(args[2]);
                    IP2 = args[3];
                    Power2 = Convert.ToInt32(args[4]);
                    RssiMin2 = Convert.ToInt32(args[5]);
                    URLTags = args[6];
                    TagServiceURL = URLTags;
                    URLGPIO = args[7];
                    Wait = Convert.ToInt32(args[8]);
                    IPDB = args[9];
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + Environment.NewLine + ex.ToString());
                    Console.ReadLine();
                    System.Environment.Exit(0);
                }

            }
        }
        private static void LeerDoble()
        {
            Task<bool> TaskConnect = new Task<bool>(() => Antenna1.Connect(true, true));
            Task<bool> TaskConnect2 = new Task<bool>(() => Antenna2.Connect(true, true));
            try
            {
                Console.WriteLine("");
                Console.Write("Conectando Antena 1..");
                TaskConnect.Start();
                while (!TaskConnect.IsCompleted)
                {
                    Console.Write(".");
                    Thread.Sleep(1000);
                }
                if (!Antenna1.Connected)
                {
                    Console.WriteLine("\nNo se pudo conectar la antena: " + IP1);
                    return;
                }
                Console.WriteLine("");
                Console.Write("Conectando Antena 2..");
                TaskConnect2.Start();
                while (!TaskConnect2.IsCompleted)
                {
                    Console.Write(".");
                    Thread.Sleep(1000);
                }
                if (!Antenna2.Connected)
                {
                    Console.WriteLine("\nNo se pudo conectar la antena: " + IP2);
                    return;
                }
                while (!tokenSource2.IsCancellationRequested && Antenna1.Connected && Antenna2.Connected)
                {
                    Conectado1 = true;
                    Conectado2 = true;
                    Console.WriteLine("\nConexión exitosa.");
                    Antenna1.Sensors = true;
                    Antenna2.Sensors = true;
                    Antenna1.EraseTime = Wait;
                    Antenna2.EraseTime = Wait;
                    Antenna1.NewReadTag += Antenna1_NewReadTag;
                    Antenna2.NewReadTag += Antenna2_NewReadTag;
                    ExitTimer.Start();
                    BlockedTimer.Elapsed += BlockedTimer_Elapsed;
                    BlockedTimer.Start();
                    flashTimer.Elapsed += flashTimer_Elapsed;
                    flashTimer.Start();
                    Antenna1.PassStatusChange += Antenna1_PassStatusChange;
                    Antenna2.PassStatusChange += Antenna2_PassStatusChange;
                    Antenna1.Sensor0Change += Antenna1_Sensor0Change;
                    Antenna2.Sensor0Change += Antenna2_Sensor0Change;
                    while (Antenna1.Connected && Conectado1 && !tokenSource2.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                }
                Conectado1 = false;
                Conectado2 = false;
            }
            catch (Exception)
            {

                Conectado1 = false;
                Conectado2 = false;
            }
            finally
            {
                Environment.Exit(0);
                if (Antenna1.Connected) DetenerAntena(Antenna1);
                Conectado1 = false;
                if (Antenna2.Connected) DetenerAntena(Antenna2);
                Conectado2 = false;
                Console.WriteLine("La antena se ha detenido.");
                TaskConnect.Dispose();
                TaskConnect2.Dispose();
            }
        }

        public static bool semaforoBloqueado = false;
        static void flashTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //bool hasTags = Antenna2.TagList.Count > 0;
            //bool f = false;
            //bool b = false;
            //Flash.TryGetValue("on", out f);
            ////SemaforoBloqueado.TryGetValue("bloqueado", out b);
            //if (f && !b)
            //{
            //    //Antenna2.TagList.Clear();
            //    CS203.SetPort0(IP2, hasTags);
            //    CS203.SetPort1(IP2, !hasTags);
            //    //Antenna2.SetPort(!hasTags, hasTags);
            //    Thread.Sleep(300);
            //    //Antenna2.SetPort(false, false);
            //    CS203.SetPort0(IP2, false);
            //    CS203.SetPort1(IP2, false);
            //    //OnReading.Clear();
            //}
            ////BloquearSemaforo();
            //if (Flash.TryGetValue("on", out f))
            //{
            //    Flash.TryUpdate("on", false, f);
            //}
            //else
            //{
            //    Flash.TryAdd("on", false);
            //}
        }
        static System.Threading.Timer _timer;
        public static ConcurrentDictionary<string, bool> SemaforoBloqueado = new ConcurrentDictionary<string, bool>();
        public static void BloquearSemaforo()
        {
            bool b = false;
            if (SemaforoBloqueado.TryGetValue("bloqueado", out b))
            {
                SemaforoBloqueado.TryUpdate("bloqueado", true, b);
            }
            else
            {
                SemaforoBloqueado.TryAdd("bloqueado", true);
            }
            semaforoBloqueado = true;
            TimeSpan span = new TimeSpan(0, 0, 0, 1);
            TimeSpan disablePeriodic = new TimeSpan(0, 0, 0, 0, -1);
            _timer = new System.Threading.Timer(checker_TimerCallback, null,
                span, disablePeriodic);
        }
        static object Lock = new object();
        private static void checker_TimerCallback(object state)
        {
            lock (Lock)
            {
                semaforoBloqueado = false;
                bool b = false;
                if (SemaforoBloqueado.TryGetValue("bloqueado", out b))
                {
                    SemaforoBloqueado.TryUpdate("bloqueado", false, b);
                }
                else
                {
                    SemaforoBloqueado.TryAdd("bloqueado", false);
                }
            }
        }
        static void Antenna2_PassStatusChange(ProcessState state)
        {
            if (state == ProcessState.E2 || state == ProcessState.S2)
            {
            }
        }

        static void Antenna1_PassStatusChange(ProcessState state)
        {
            if (state == ProcessState.E2 || state == ProcessState.S2)
            {
            }
        }

        static void BlockedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var copyEntrada = new ConcurrentDictionary<string, int>(ListaEntrada);
            var copySalida = new ConcurrentDictionary<string, int>(ListaSalida);
            foreach (var item in copyEntrada)
            {
                int time = item.Value - 1;
                if (time < 1)
                {
                    ListaEntrada.TryRemove(item.Key, out time);
                }
                else
                {
                    ListaEntrada.TryUpdate(item.Key, time, item.Value);
                }
            }
            foreach (var item in copySalida)
            {
                int time = item.Value - 1;
                if (time < 1)
                {
                    ListaSalida.TryRemove(item.Key, out time);
                }
                else
                {
                    ListaSalida.TryUpdate(item.Key, time, item.Value);
                }
            }
        }

        static void Antenna2_Sensor0Change(bool on)
        {
            if (On == on)
            {
                return;
            }
            else
            {
                On = on;
            }
            if (on)
            {
                return;
            }
            bool f = false;
            if (Flash.TryGetValue("on", out f))
            {
                Flash.TryUpdate("on", true, f);
            }
            else
            {
                Flash.TryAdd("on", true);
            }
            var lista = new List<TagData>(Antenna2.TagList);
            string lecturas = DateTime.Now.ToString() + "\t" + Antenna1.IP + "\t\t\t" + new String('-', 24) + "\tLeyendo Movimientos";
            foreach (TagData item in lista)
            {
                lecturas += "\n\t\t\t\t\t\t\t\t" + item.EPC;
            }
            lecturas += "\n\t\t\t\t\t\t\t\t" + new String('-', 24);


            bool hasTags = lista.Count > 0;
            //SemaforoBloqueado.TryGetValue("bloqueado", out b);
            //Antenna2.TagList.Clear();
            CS203.SetPort0(IP2, hasTags);
            CS203.SetPort1(IP2, !hasTags);
            //Antenna2.SetPort(!hasTags, hasTags);
            Thread.Sleep(300);
            //Antenna2.SetPort(false, false);
            CS203.SetPort0(IP2, false);
            CS203.SetPort1(IP2, false);
            //OnReading.Clear();
        }

        public static ConcurrentDictionary<string, bool> Flash = new ConcurrentDictionary<string, bool>();

        public static volatile bool On = true;
        static void Antenna1_Sensor0Change(bool on)
        {

        }


        public static int ExitTIme = 5;
        static void AddOnReading(string epc)
        {
            if (!OnReading.ContainsKey(epc))
            {
                OnReading.TryAdd(epc, ExitTIme);
            }
            else
            {
                int time = ExitTIme;
                OnReading.TryGetValue(epc, out time);
                OnReading.TryUpdate(epc, ExitTIme, time);
            }
        }

        public static ConcurrentDictionary<string, int> ListaEntrada = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> ListaSalida = new ConcurrentDictionary<string, int>();
        public static int Erasetime = 10;
        public static void PublishEntrada(TagData tag)
        {
            if (!ListaEntrada.ContainsKey(tag.EPC))
            {
                Console.WriteLine(DateTime.Now.ToString() + "\t" + IP1 + "\tEntrada\t\t" + tag.EPC);
                RestClient.PublishTag(TagServiceURL, IPDB, new RestClient.RestTag { epc = tag.EPC, direction = "0", ip = IP1, rssi = tag.RSSI.ToString(), timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                ListaEntrada.TryAdd(tag.EPC, Erasetime);
            }
            else
            {
                int t = 0;
                ListaEntrada.TryGetValue(tag.EPC, out t);
                ListaEntrada.TryUpdate(tag.EPC, Erasetime, t);
            }
            if (!ListaSalida.ContainsKey(tag.EPC))
            {
                int t = 0;
                ListaSalida.TryRemove(tag.EPC, out t);
            }
        }
        public static void PublishSalida(TagData tag)
        {
            if (!ListaSalida.ContainsKey(tag.EPC))
            {
                Console.WriteLine(DateTime.Now.ToString() + "\t" + IP1 + "\tSalida\t\t" + tag.EPC);
                RestClient.PublishTag(TagServiceURL, IPDB, new RestClient.RestTag { epc = tag.EPC, direction = "1", ip = IP1, rssi = tag.RSSI.ToString(), timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                ListaSalida.TryAdd(tag.EPC, Erasetime);
            }
            else
            {
                int t = 0;
                ListaSalida.TryGetValue(tag.EPC, out t);
                ListaSalida.TryUpdate(tag.EPC, Erasetime, t);
            }
            if (!ListaEntrada.ContainsKey(tag.EPC))
            {
                int t = 0;
                ListaEntrada.TryRemove(tag.EPC, out t);
            }

        }


        private static void Antenna1_NewReadTag(TagData tag)
        {
            PublishSalida(tag);
            //Console.WriteLine(DateTime.Now.ToString() + "\t" + IP1 + "\t" + tag.EPC + "\t< Antena 1");
        }


        private static void Antenna2_NewReadTag(TagData tag)
        {
            PublishEntrada(tag);
            //Console.WriteLine(DateTime.Now.ToString() + "\t" + IP2+ "\t" + tag.EPC + "\t< Antena 2");
        }

        private static void DetenerAntena(CS203 antena)
        {
            if (antena == null) return;
            Thread.Sleep(1000);
            antena.Stop();
            Thread.Sleep(1000);
            Conectado1 = false;
        }

    }
}
