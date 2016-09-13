using HTKCSL;
using HTKRestClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MDW_AntenaPrincipalDoble2
{
    class Program
    {
        public static bool Conectado1 { get; set; }
        public static bool Conectado2 { get; set; }
        public static CS203 Antenna1;
        public static CS203 Antenna2;
        static CancellationTokenSource tokenSource2;
        static CancellationToken ct;
        public static string IP1 { get; set; }
        public static string IP2 { get; set; }
        public static int Power1 { get; set; }
        public static int Power2 { get; set; }
        public static string  URL { get; set; }

        public static ConcurrentDictionary<string, TagData> TagList = new ConcurrentDictionary<string, TagData>();
        public static System.Timers.Timer TagListTimer = new System.Timers.Timer(1000);
        static void Main(string[] args)
        {
            IP1 = "192.168.1.231";
            IP2 = "192.168.1.237";
            Antenna1 = new CS203(IP1, 250);
            Antenna2 = new CS203(IP2, 300);
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
                    Antenna1.EraseTime = 15;
                    Antenna2.EraseTime = 15;
                    Antenna1.NewReadTag += Antenna1_NewReadTag;
                    Antenna2.NewReadTag += Antenna2_NewReadTag;
                    Antenna1.Sensor0Change += Antenna1_Sensor0Change;
                    TagListTimer.Elapsed += TagListTimer_Elapsed;
                    TagListTimer.Start();
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

        static void TagListTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.Clear();
            List<string> entrada = new List<string>();
            List<string> salida = new List<string>();

            var copy = new ConcurrentDictionary<string, TagData>(TagList);
            foreach (var item in copy)
            {
                int time = item.Value.EraseTime-1;
                if (time > 0)
                {
                    var newTag = item.Value;
                    newTag.EraseTime = time;
                    TagList.TryUpdate(item.Value.EPC, newTag, item.Value);
                }
                else
                {
                    TagData value = new TagData();
                    TagList.TryRemove(item.Value.EPC, out value);
                }
                if (item.Value.IP == IP1)
                {
                    entrada.Add(item.Value.EPC);
                }
                if (item.Value.IP == IP2)
                {
                    salida.Add(item.Value.EPC);
                }
            }

            Console.WriteLine("\nEntrada:");
            foreach (var item in entrada)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("\nSalida:");
            foreach (var item in salida)
            {
                Console.WriteLine(item); 
            }
        }

        private static void Antenna1_Sensor0Change(bool on)
        {
            
        }

        private static void Antenna2_NewReadTag(TagData tag)
        {
            AddToTagList(tag);
        }

        private static void Antenna1_NewReadTag(TagData tag)
        {
            AddToTagList(tag);
        }

        private static void AddToTagList(TagData tag)
        {
            TagData lectura = tag;
            tag.EraseTime = 5;
            lectura.EraseTime = 5;
            if (TagList.ContainsKey(tag.EPC))
            {
                TagList.TryGetValue(tag.EPC, out lectura);
                TagList.TryUpdate(tag.EPC, tag, lectura);
            }
            else
            {
                TagList.TryAdd(tag.EPC, tag);
            }
        }
       
        private static void DetenerAntena(CS203 antena)
        {
            if (antena == null) return;
            Thread.Sleep(1000);
            antena.Stop();
            Thread.Sleep(1000);
            Conectado1 = false;
        }
        private static void Mensaje(string ip, string dir, string epc, string mensaje)
        {
            string direccion = "";
            if (dir == "0") direccion = "Entrada";
            if (dir == "1") direccion = "Salida";
            string cadena = DateTime.Now.ToString() + "\t" + ip + "\t" + direccion + epc + "\t" + mensaje;
            Console.WriteLine(cadena);
        }
    }

       
}
