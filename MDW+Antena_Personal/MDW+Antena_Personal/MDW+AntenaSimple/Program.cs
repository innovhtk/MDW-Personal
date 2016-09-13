using HTKCSL;
using HTKRestClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MDW_AntenaSimple
{
    class Program
    {
        public static CS203 Antenna;
        public static string IP { get; set; }
        public static int Power { get; set; }
        public static int RssiMin { get; set; }
        public static string URLTags { get; set; }
        public static string URLGPIO { get; set; }
        public static string IPBD { get; set; }
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
        public static ConcurrentDictionary<string, TagState> HoldTags = new ConcurrentDictionary<string, TagState>();
        public static System.Timers.Timer Wait = new System.Timers.Timer(1000);
        public static System.Timers.Timer WaitListTimer = new System.Timers.Timer(100);
        public static System.Timers.Timer FlashTimer = new System.Timers.Timer(300);
        public static System.Timers.Timer SensorTimer = new System.Timers.Timer(100);
        public static ProcessState LastState = ProcessState.Inactivo;

        public static ConcurrentDictionary<string, Registro> EstadoTag = new ConcurrentDictionary<string, Registro>();

        public class Registro
        {
            public Estado estado { get; set; }
            public int horas { get; set; }

            public Registro()
            {
                horas = 0;
                estado = Estado.Neutro;
            }
            public Registro(Estado _estado)
            {
                horas = 0;
                estado = _estado;
            }
        }
        public enum Estado
        {
            Neutro,
            Dentro,
            Fuera
        }
        static System.Threading.Timer _timer;
        public static void Start1HourTimer()
        {
            TimeSpan span = new TimeSpan(0, 1, 0, 0);
            TimeSpan disablePeriodic = new TimeSpan(0, 0, 0, 0, -1);
            _timer = new System.Threading.Timer(timer_TimerCallback, null,
                span, disablePeriodic);
        }

        public static void timer_TimerCallback(object state)
        {
            var copia = EstadoTag;
            foreach (var item in copia)
            {
                Registro actual = item.Value;
                actual.horas++;
                if (actual.horas > 14)
                {
                    if (actual.estado == Estado.Dentro)
                    {
                        EstadoTag.TryUpdate(item.Key, new Registro(Estado.Fuera), item.Value);
                        var tag = new TagData() { EPC = item.Key, EraseTime = 0, IP = IP, RSSI = 80 };
                        try
                        {
                            Console.Write("\n" + DateTime.Now.ToString() + "\t");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(tag.EPC);
                            Console.ResetColor();
                            Console.Write("\t< Salida");
                            PublishMovement(PassType.Salida, tag.EPC, tag.RSSI);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("\n" + DateTime.Now.ToString() + "\t" + tag.EPC + "\t[Publicación fallida]");
                        }

                    }
                }
            }
            _timer.Dispose();
            Start1HourTimer();
        }
        public static void UpdateEstado(string epc, Estado estado)
        {
            var reg = new Registro();
            if (EstadoTag.TryGetValue(epc, out reg))
            {
                EstadoTag.TryUpdate(epc, reg, new Registro(estado));
            }
            else
            {
                EstadoTag.TryAdd(epc, new Registro(estado));
            }
        }
        static void Main(string[] args)
        {
            GetData(args);
            Console.SetBufferSize(650, 300);
            Console.SetWindowPosition(0, 5);
            Console.SetWindowSize(160, 12);


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
                    Antenna.SimpleExit = simplePass;
                    Antenna.PassStatusChange += Antenna_PassStatusChange;
                    if (IP == "192.168.25.231")
                    {
                        Start1HourTimer();
                    }
                    while (Antenna.Connected)
                    {
                        Thread.Sleep(100);
                    }
                    Antenna.Connected = false;
                    Console.Title = IP + " Conectada";
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
                Antenna.PassStatusChange -= Antenna_PassStatusChange;
                Antenna.Stop();
            }
        }


        static bool pass = false;
        static object MyLocker = new object();
        public static bool Passing
        {
            get
            {
                lock (MyLocker)
                {
                    return pass;
                }
            }
            set
            {
                lock (MyLocker)
                {
                    pass = value;
                }

            }
        }


        private static void Wait_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var item in HoldTags)
            {
                item.Value.CountDown();
            }
        }
        private static void CountDownPass(string epc)
        {
            Thread.Sleep(100);
            TagState state = new TagState();
            HoldTags.TryGetValue(epc, out state);
            TagState newstate = state;
            newstate.CountDown();
            HoldTags.TryUpdate(epc, newstate, state);
        }
        private static void DeletePass(string epc)
        {
            Thread.Sleep(100);
            TagState tagstate = new TagState();
            HoldTags.TryRemove(epc, out tagstate);
        }
        private static void Antenna_PassStatusChange(ProcessState state)
        {
            //if (LastState == ProcessState.E3 && state == ProcessState.Inactivo)
            //{
            //    Antenna_Entrance(Antenna.TagList.ToArray());
            //}
            //if (LastState == ProcessState.S3 && state == ProcessState.Inactivo)
            //{
            //    Antenna_Exit(Antenna.TagList.ToArray());
            //}
            if (state == ProcessState.Inactivo)
            {
                Passing = false;
            }
            if (state == ProcessState.E1 && !Passing)
            {
                Console.WriteLine("Entrada");
                Entrance(Antenna.TagList.ToArray());
                //Antenna.TagList.Clear();
            }
            if (state == ProcessState.S1 && !Passing)
            {
                Console.WriteLine("Salida");
                Exit(Antenna.TagList.ToArray());
                //Antenna.TagList.Clear();
            }
            LastState = state;


        }
        public static ConcurrentDictionary<string, bool> inUse = new ConcurrentDictionary<string, bool>();
        static readonly object _object = new object();

        public static void Exit(TagData[] tags)
        {
            lock (_object)
            {
                //Antenna.TagList.Clear();
                if (tags.Length < 1)
                {
                    Console.WriteLine("\n" + DateTime.Now.ToString() + "\t[Sin Tag]\t\t\t< Salida");
                    Console.WriteLine("\n" + "La IP es: " + Antenna.IP);

                    return;
                }
                List<string> releasethis = new List<string>();
                foreach (var tag in tags)
                {
                    bool used = false;
                    if (inUse.TryGetValue(tag.EPC, out used))
                    {
                        continue;
                    }
                    else
                    {
                        inUse.TryAdd(tag.EPC, true);
                        releasethis.Add(tag.EPC);
                    }
                    if (tag.RSSI < RssiMin)
                    {
                        continue;
                    }
                    if (tag.EPC != "")
                    {
                        try
                        {
                            Console.Write("\n" + DateTime.Now.ToString() + "\t");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(tag.EPC);
                            Console.ResetColor();
                            Console.Write("\t< Salida");
                            PublishMovement(PassType.Salida, tag.EPC, tag.RSSI);
                            UpdateEstado(tag.EPC, Estado.Fuera);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("\n" + DateTime.Now.ToString() + "\t" + tag.EPC + "\t[Publicación fallida]");
                        }
                    }
                }
                Thread.Sleep(2000);
                foreach (var epc in releasethis)
                {
                    bool used = false;
                    inUse.TryRemove(epc, out used);
                }
            }
        }

        public static void Entrance(TagData[] tags)
        {
            //Antenna.TagList.Clear();
            if (tags.Length < 1)
            {
                Console.WriteLine("\n" + DateTime.Now.ToString() + "\t[Sin Tag]\t\t\t< Entrada");
                Console.WriteLine("\n" + "La IP es: " + Antenna.IP);
                return;
            }
            foreach (var tag in tags)
            {
                if (tag.RSSI < RssiMin)
                {
                    continue;
                }
                if (tag.EPC != "")
                {
                    try
                    {
                        Console.Write("\n" + DateTime.Now.ToString() + "\t");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(tag.EPC);
                        Console.ResetColor();
                        Console.Write("\t< Entrada");
                        PublishMovement(PassType.Entrada, tag.EPC, tag.RSSI);
                        UpdateEstado(tag.EPC, Estado.Dentro);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("\n" + DateTime.Now.ToString() + "\t" + tag.EPC + "\t[Publicación fallida]");
                    }
                }
            }
        }

        private static volatile object _objectLock = new object();
        private static void flashred(bool red)
        {
            lock (_objectLock)
            {
                if (Antenna.IP.ToString() == "192.168.1.231")
                {
                    return;
                }
                try
                {
                    Antenna.SetPort(IP, !red, red);
                    Thread.Sleep(200);
                    Antenna.SetPort(IP, false, false);
                }
                catch (Exception ) { }
            }

        }
        private static void PublishMovement(PassType passType, string epc, float rssi)
        {
            TagState state = new TagState();
            try
            {
                if (HoldTags != null && HoldTags.Count > 0)
                {
                    if (HoldTags.TryGetValue(epc, out state))
                    {
                        Console.Write(String.Format("\tLast> Pass:{1} EraseTime: {2}", epc, state.Pass.ToString(), state.EraseTime.ToString()));
                        if (state.EraseTime > 15 - waitTime)
                        {
                            Console.WriteLine("\n" + DateTime.Now.ToString() + "\t" + epc + "\t[" + passType.ToString() + " rápida, no publicada]");
                            return;
                        }
                        if (state.Pass == passType)
                        {
                            Console.WriteLine("\n" + DateTime.Now.ToString() + "\t" + epc + "\t[" + passType.ToString() + " duplicada, no publicada]");
                            return;
                        }
                    }
                }
                else
                {
                    Console.Write("\tPrimera lectura");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR: HoldTags");
            }
            try
            {
                var s = new TagState();
                if (HoldTags.TryGetValue(epc, out s))
                {
                    var news = new TagState();
                    news.Pass = passType;
                    news.EraseTime = waitTime;
                    HoldTags.TryUpdate(epc, news, s);
                }
                else
                {
                    state = new TagState(passType, waitTime);
                    if (!HoldTags.TryAdd(epc, state))
                    {
                        Console.WriteLine("\nNo se pudo agregar " + epc + " a HoldTags");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ": " + ex.ToString());
                Console.WriteLine("\nERROR: Add HoldTag");
            }

            try
            {
                int count = 5;
                if (waitlist.TryGetValue(epc, out count))
                {
                    return;
                }
                else
                {
                    waitlist.TryAdd(epc, 5);
                }
                string movement = "0";
                if (passType == PassType.Entrada) movement = "0";
                if (passType == PassType.Salida) movement = "1";
                Task.Factory.StartNew(() =>
                {
                    RestClient.PublishTag(URLTags, IPBD, new RestClient.RestTag
                    {
                        direction = movement,
                        epc = epc,
                        ip = IP,
                        rssi = rssi.ToString(),
                        timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime())
                    });
                });
            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR: RestClient.PublishTag");
            }

        }
        public static ConcurrentDictionary<string, int> waitlist = new ConcurrentDictionary<string, int>();
        public static int waitTime = 10;

        static bool simplePass = false;
        private static void GetData(string[] args)
        {
#if DEBUG
            //IP = "192.168.25.203";
            //Power = 250;
            //RssiMin = 10;
            //URLTags = "http://192.168.1.62/api/";
            //URLGPIO = "http://192.168.1.62/api/";
            //waitTime = 1;
            //return;
#endif
            if (args.Length < 6)
            {
                System.Environment.Exit(0);
            }
            else
            {
                try
                {
                    IP = args[0];
                    Power = Convert.ToInt32(args[1]);
                    RssiMin = Convert.ToInt32(args[2]);
                    URLTags = args[3];
                    URLGPIO = args[4];
                    if (args.Length > 5)
                    {
                        try
                        {
                            waitTime = Convert.ToInt32(args[5]);
                        }
                        catch (Exception)
                        {
                            waitTime = 10;
                        }
                    }
                    if (args.Length > 6)
                    {
                        if (args[6] == "simplepass")
                        {
                            simplePass = true;
                        }
                        IPBD = args[7];
                    }
                    if (Power < 0 || Power > 300) System.Environment.Exit(0);
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
