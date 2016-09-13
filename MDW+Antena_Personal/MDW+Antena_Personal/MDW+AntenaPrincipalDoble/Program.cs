using HTKCSL;
using HTKRestClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace MDW_AntenaPrincipalDoble
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
        public static string IPDB { get; set; }

        static CancellationTokenSource tokenSource2;
        static CancellationToken ct;
        public static ConcurrentDictionary<string, int> ToExit = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> ToEntrance = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> OnReading = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> Blocked = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> Trabajando = new ConcurrentDictionary<string, int>();
        public static ConcurrentDictionary<string, int> Espera = new ConcurrentDictionary<string, int>();
        public static System.Timers.Timer ExitTimer = new System.Timers.Timer(600000);
        public static System.Timers.Timer EntranceTimer = new System.Timers.Timer(10000);
        public static System.Timers.Timer BlockedTimer = new System.Timers.Timer(1000);
        public static System.Timers.Timer EsperaTimer = new System.Timers.Timer(1000);
        public static int ExitTime = 5;
        public static int EntranceTime = 2;
        public static volatile bool On = false;
        public static volatile bool Publishing = false;
        private static bool ya_estaba_prendido;

        static void Main(string[] args)
        {
            Console.WriteLine("MDW+AntenaPrincipalDoble 5.2");
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
            Console.WriteLine(args.Length + " argumentos");

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
                    Console.WriteLine(String.Format("IP1: {0}, Power1: {1}, RSSI1: {2}, IP2: {3}, Power2: {4}, RSSI2: {5}, URL: {6}, \nTiempo entrada-entrada: {7}\nTiempo saida-salida: {8}\nTiempo entrada-salida: {9}\nTiempo salida-entrada: {10}, \nIPDB: {11}",
                        args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[8], args[9], args[10], args[11], args[12])
                        );
                    IP1 = args[0];
                    Power1 = Convert.ToInt32(args[1]);
                    RssiMin1 = Convert.ToInt32(args[2]);
                    IP2 = args[3];
                    Power2 = Convert.ToInt32(args[4]);
                    RssiMin2 = Convert.ToInt32(args[5]);
                    URLTags = args[6];
                    TagServiceURL = URLTags;
                    URLGPIO = args[7];
                    RestClient.TiempoEntradaEntrada = Convert.ToInt32(args[8]);
                    RestClient.TiempoSalidaSalida = Convert.ToInt32(args[9]);
                    RestClient.TiempoEntradaSalida = Convert.ToInt32(args[10]);
                    RestClient.TiempoSalidaEntrada = Convert.ToInt32(args[11]);
                    IPDB = args[12];
                    string url = "";
                    if (IPDB.Contains("http"))
                    {
                        url = IPDB;
                    }
                    else
                    {
                        url = "http://" + IPDB + "/api/";
                    }
                    Console.Write("Escriba la IP de la base de datos: ");
                    URLTags = url;
                    TagServiceURL = url;
                    URLGPIO = url;
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
                    Antenna1.EraseTime = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15; 
                    Antenna2.EraseTime = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15; 
                    Antenna1.NewReadTag += Antenna1_NewReadTag;
                    Antenna2.NewReadTag += Antenna2_NewReadTag;
                    ExitTimer.Elapsed += ExitTimer_Elapsed;
                    ExitTimer.Start();
                    EntranceTimer.Elapsed += EntranceTimer_Elapsed;
                    EntranceTimer.Start();
                    BlockedTimer.Elapsed += BlockedTimer_Elapsed;
                    BlockedTimer.Start();
                    EsperaTimer.Elapsed += EsperaTimer_Elapsed;
                    EsperaTimer.Start();
                    Antenna1.Sensor0Change += Antenna1_Sensor0Change;
                    Antenna1.Sensor1Change += Antenna1_Sensor1Change;
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

        private static void Antenna1_Sensor1Change(bool on)
        {
            //bool on = Antenna2.GetPort1();
            //Thread.Sleep(20);
            //bool on1 = Antenna2.GetPort1();
            //Thread.Sleep(20);
            //bool on2 = Antenna2.GetPort1();
            //on = on || on1 || on2;
            if(on) Console.WriteLine("Se detectó paso por el sensor");
            if (ya_estaba_prendido && !on) ya_estaba_prendido = false;
            if (!on || ya_estaba_prendido) return;
            ya_estaba_prendido = true;
            var lista = new List<TagData>(Antenna1.TagList);
            foreach (TagData item in lista)
            {
                if (item.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 1");
                if (Blocked.ContainsKey(item.EPC)) continue;
                if (item.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 2");
                AddToEntrance(item.EPC);
                if (item.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 3");
            }
            var copy = new ConcurrentDictionary<string, int>(ToEntrance);
            foreach (var item in copy)
            {
                if (item.Key == "AABC2016021813540000D71C") Console.WriteLine("Paso 4");
                if (Blocked.ContainsKey(item.Key)) continue;
                if (item.Key == "AABC2016021813540000D71C") Console.WriteLine("Paso 5");
                lista.Add(new TagData { EPC = item.Key, EraseTime = 2, IP = Antenna1.IP, RSSI = 80 });
                if (item.Key == "AABC2016021813540000D71C") Console.WriteLine("Paso 6");
            }

            if (lista.Count < 1)
            {
                lista.Add(new TagData() { EPC = "000000000000000000000000", EraseTime = 1, IP = Antenna1.IP, RSSI = 80 });
            }
            Console.WriteLine("Lecturas:");
            string lecturas = DateTime.Now.ToString() + "\t" + Antenna1.IP + "\t\t\t" + new string('-', 24) + "\tLeyendo Tags";
            foreach (TagData item in lista)
            {
                lecturas += "\n\t\t\t\t\t\t\t\t" + item.EPC;
                if (item.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 7");
            }
            lecturas += "\n\t\t\t\t\t\t\t\t" + new string('-', 24);
            Console.WriteLine(lecturas);
            Console.WriteLine("En lista de salida:");
            string enlistasalida = DateTime.Now.ToString() + "\t" + Antenna1.IP + "\t\t\t" + new string('-', 24) + "\tLeyendo en lista de salida";
            foreach (var item in ToExit)
            {
                enlistasalida += "\n\t\t\t\t\t\t\t\t" + item.Key;
            }
            enlistasalida += "\n\t\t\t\t\t\t\t\t" + new String('-', 24);
            Console.WriteLine(enlistasalida);

            foreach (TagData t in lista)
            {
                if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 8");
                if (OnReading.ContainsKey(t.EPC))
                {
                    continue;
                }
                if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 9");
                AddOnReading(t.EPC);
                if (ToExit.ContainsKey(t.EPC))
                {
                    if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 10");
                    if (Blocked.ContainsKey(t.EPC)) continue;
                    if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 11");
                    PublishSalida(t);
                    int time = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15;
                    int i = 0;
                    ToExit.TryRemove(t.EPC, out i);
                    Blocked.TryAdd(t.EPC, time);
                    if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 12");
                }
                else
                {
                    if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 13");
                    if (Blocked.ContainsKey(t.EPC)) continue;
                    PublishEntrada(t);
                    int time = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15;
                    Blocked.TryAdd(t.EPC, time);
                    if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 14");
                }
                int val = 0;
                ToExit.TryRemove(t.EPC, out val);
                ToEntrance.TryRemove(t.EPC, out val);
                if (t.EPC == "AABC2016021813540000D71C") Console.WriteLine("Paso 15");
            }
        }

        static void EsperaTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var copy = new ConcurrentDictionary<string, int>(Espera);
            foreach (var tag in copy)
            {
                int time = tag.Value - 1;
                if (time < 1)
                {
                    Espera.TryRemove(tag.Key, out time);
                }
                else
                {
                    Espera.TryUpdate(tag.Key, time, tag.Value);
                }
            }
        }
        static void BlockedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var copy = new ConcurrentDictionary<string, int>(Blocked);
            foreach (var tag in copy)
            {
                int time = tag.Value - 1;
                if (time < 1)
                {
                    Blocked.TryRemove(tag.Key, out time);
                }
                else
                {
                    Blocked.TryUpdate(tag.Key, time, tag.Value);
                }
            }
        }
        static void Antenna1_Sensor0Change(bool on)
        {
            //Console.WriteLine(on);
            //Console.WriteLine("\nSe detectó paso por la antena\n");
            //var tagcopy = new List<TagData>(Antenna1.TagList);
            //foreach (TagData item in tagcopy)
            //{
            //    AddToEntrance(item.EPC);
            //}
            //var copy = new ConcurrentDictionary<string, int>(ToEntrance);
            //var lista = new List<TagData>();
            //foreach (var item in copy)
            //{
            //    lista.Add(new TagData { EPC = item.Key, EraseTime = 2, IP = Antenna1.IP, RSSI = 80 });
            //}
            //if (On == on)
            //{
            //    return;
            //}
            //else
            //{
            //    On = on;
            //}
            //if (!on)
            //{

            //    Publishing = false;
            //    OnReading.Clear();
            //    Console.WriteLine(DateTime.Now.ToString() + "\t\t\t\t\t\t\t\t\tLimpiando movimientos");
            //    return;
            //}
            //else
            //{
            //    Publishing = true;
            //}
            //if (lista.Count < 1)
            //{
            //    lista.Add(new TagData() { EPC = "000000000000000000000000", EraseTime = 1, IP = Antenna1.IP, RSSI = 80 });
            //}
            //string lecturas = DateTime.Now.ToString() + "\t" + Antenna1.IP + "\t\t\t" + new String('-', 24) + "\tLeyendo Movimientos";
            //foreach (TagData item in lista)
            //{
            //    lecturas += "\n\t\t\t\t\t\t\t\t" + item.EPC;
            //}
            //lecturas += "\n\t\t\t\t\t\t\t\t" + new String('-', 24);
            //Console.WriteLine(lecturas);

            //string enlistasalida = DateTime.Now.ToString() + "\t" + Antenna1.IP + "\t\t\t" + new String('-', 24) + "\tLeyendo en lista de salida";
            //foreach (var item in ToExit)
            //{
            //    enlistasalida += "\n\t\t\t\t\t\t\t\t" + item.Key;
            //}
            //enlistasalida += "\n\t\t\t\t\t\t\t\t" + new String('-', 24);
            //Console.WriteLine(enlistasalida);

            //foreach (TagData t in lista)
            //{
            //    if (OnReading.ContainsKey(t.EPC))
            //    {
            //        continue;
            //    }
            //    AddOnReading(t.EPC);
            //    if (ToExit.ContainsKey(t.EPC))
            //    {
            //        if (Blocked.ContainsKey(t.EPC)) continue;
            //        PublishSalida(t);
            //        int time = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15;
            //        int i = 0;
            //        ToExit.TryRemove(t.EPC, out i);
            //        Blocked.TryAdd(t.EPC, time);
            //    }
            //    else
            //    {
            //        if (Blocked.ContainsKey(t.EPC)) continue;
            //        PublishEntrada(t);
            //        int time = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15;
            //        Blocked.TryAdd(t.EPC, time);
            //    }
            //    //int val = 0;
            //    //ToExit.TryRemove(t.EPC, out val);
            //    //ToEntrance.TryRemove(t.EPC, out val);
            //}
        }
        static void EntranceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var copy = new ConcurrentDictionary<string, int>(ToEntrance);
            foreach (var tag in copy)
            {
                int time = tag.Value - 1;
                if (time < 1)
                {
                    ToEntrance.TryRemove(tag.Key, out time);
                }
                else
                {
                    ToEntrance.TryUpdate(tag.Key, time, tag.Value);
                }
            }
        }
        static void ExitTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var copy = new ConcurrentDictionary<string, int>(ToExit);
            foreach (var tag in copy)
            {
                int time = tag.Value - 1;
                if (time < 1)
                {
                    ToExit.TryRemove(tag.Key, out time);
                }
                else
                {
                    ToExit.TryUpdate(tag.Key, time, tag.Value);
                }
            }
        }
        static void AddToEntrance(string epc)
        {
            if (!ToEntrance.ContainsKey(epc))
            {
                ToEntrance.TryAdd(epc, EntranceTime);
            }
            else
            {
                int time = EntranceTime;
                ToEntrance.TryGetValue(epc, out time);
                ToEntrance.TryUpdate(epc, EntranceTime, time);
            }
        }
        static void AddToExit(string epc)
        {
            if (!ToExit.ContainsKey(epc))
            {
                ToExit.TryAdd(epc, ExitTime);
                Console.WriteLine(DateTime.Now.ToString() + "\t" + IP2 + "\t\t\t" + epc + "\tAgregado a lista de salida");
            }
            else
            {
                int time = ExitTime;
                ToExit.TryGetValue(epc, out time);
                ToExit.TryUpdate(epc, ExitTime, time);
                Console.WriteLine(DateTime.Now.ToString() + "\t" + IP2 + "\t\t\t" + epc + "\tActualizado en lista de salida");
            }
        }
        static void AddOnReading(string epc)
        {
            if (!OnReading.ContainsKey(epc))
            {
                OnReading.TryAdd(epc, ExitTime);
            }
            else
            {
                int time = ExitTime;
                OnReading.TryGetValue(epc, out time);
                OnReading.TryUpdate(epc, ExitTime, time);
            }
        }
        public static bool AddTrabajando(string epc)
        {
            int a = 0;
            if (Trabajando.ContainsKey(epc))
            {
                return false;
            }
            Trabajando.TryAdd(epc, a);
            return true;
        }
        public static void AddEspera(string epc)
        {
            int a = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15;
            Trabajando.TryRemove(epc, out a);
            a = RestClient.TiempoSalidaEntrada > 0 ? RestClient.TiempoSalidaEntrada : 15; 
            Espera.TryAdd(epc, a);
        }
        public static bool Disponible(string epc)
        {
            int a = 0;
            bool trabajando = Trabajando.TryGetValue(epc, out a);
            bool espera = Espera.TryGetValue(epc, out a);

            return !trabajando && !espera;
        }
        public static void PublishEntrada(TagData tag)
        {
            if (Disponible(tag.EPC))
            {
                AddTrabajando(tag.EPC);
                RestClient.PublishTag(TagServiceURL, IPDB, new RestClient.RestTag { epc = tag.EPC, direction = "0", ip = IP1, rssi = tag.RSSI.ToString(), timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                AddEspera(tag.EPC);
                TagData t = Antenna1.TagList.Find(x => x.EPC == tag.EPC);
                if (t != null)
                    Antenna1.TagList.Remove(t);
            }
        }
        public static void PublishSalida(TagData tag)
        {

            if (Disponible(tag.EPC))
            {
                AddTrabajando(tag.EPC);
                RestClient.PublishTag(TagServiceURL, IPDB, new RestClient.RestTag { epc = tag.EPC, direction = "1", ip = IP1, rssi = tag.RSSI.ToString(), timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                AddEspera(tag.EPC);
                TagData t = Antenna1.TagList.Find(x => x.EPC == tag.EPC);
                if (t != null)
                    Antenna1.TagList.Remove(t);
            }
        }
        private static void Antenna1_NewReadTag(TagData tag)
        {
            if (Publishing)
            {
                if (OnReading.ContainsKey(tag.EPC))
                {
                    return;
                }
                if (ToExit.ContainsKey(tag.EPC))
                {
                    if (Blocked.ContainsKey(tag.EPC)) return;
                    PublishSalida(tag);
                    Blocked.TryAdd(tag.EPC, 45);
                }
                else
                {
                    if (Blocked.ContainsKey(tag.EPC)) return;
                    PublishEntrada(tag);
                    Blocked.TryAdd(tag.EPC, 45);
                }
                int val = 0;
                ToExit.TryRemove(tag.EPC, out val);
                AddOnReading(tag.EPC);
            }
            else
            {
                AddToEntrance(tag.EPC);
            }
        }
        private static void Antenna2_NewReadTag(TagData tag)
        {
            AddToExit(tag.EPC);
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
