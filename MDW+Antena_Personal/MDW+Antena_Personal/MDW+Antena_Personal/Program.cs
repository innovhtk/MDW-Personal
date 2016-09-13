using HTKCSL;
using HTKRestClient;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MDW_Antena_Personal
{
    class Program
    {
        public enum TipoAntena
        {
            Simple,
            Doble
        }
        public static TipoAntena Tipo = TipoAntena.Simple;
        public static string IP1 { get; set; }
        public static string IP2 { get; set; }
        public static int Potencia1 { get; set; }
        public static int Potencia2 { get; set; }
        public static bool Actuadores { get; set; }
        public static CS203 Antena1 { get; set; }
        public static CS203 Antena2 { get; set; }
        public static bool Conectado1 { get; set; }
        public static bool Conectado2 { get; set; }
        public static float LimiteDeLectura { get; set; }
        public static string TagServiceURL = "http://192.168.1.219/api/Tag";
        public static string ActuatorServiceURL = "http://webservice.assetsapp.com/EPCNovag/api/";
        public static ConcurrentDictionary<string, PassType> HoldTags = new ConcurrentDictionary<string, PassType>();
        public static ConcurrentDictionary<string, int> WaitTags = new ConcurrentDictionary<string, int>();
        public static System.Timers.Timer waitTimer = new System.Timers.Timer(1000);
        public enum PassType
        {
            Entrada, Salida, None
        }
        static CancellationTokenSource tokenSource2;
        static CancellationToken ct;
        public enum AntenaStep
        {
            Antena1, Antena2, None
        }
        public static ConcurrentDictionary<string, ProcessState> DobleTagPass = new ConcurrentDictionary<string, ProcessState>();
        static void Main(string[] args)
        {
            bool exit = false;
            while (!exit)
            {
                Inicializar();
                SeleccionarAntena();
                waitTimer.Elapsed += waitTimer_Elapsed;
                waitTimer.Start();
                switch (Tipo)
                {
                    case TipoAntena.Simple:
                        ConfigurarSimple();
                        ConectarSimple();
                        break;
                    case TipoAntena.Doble:
                        ConfigurarDoble();
                        ConectarDoble();
                        break;
                    default:
                        break;
                }
                DetenerAntena(Antena1);
                DetenerAntena(Antena2);

                Console.WriteLine("");
                Console.WriteLine("(1) Reiniciar (2) Salir");
                Console.Write("Ingrese una acción a realizar: ");
                string resp = Console.ReadLine();
                Console.WriteLine("");
                exit = resp == "1" ? false : true;
            }
            Console.WriteLine("La aplicación ha concluido...");
            Console.ReadLine();

        }

        static void waitTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var item in WaitTags)
            {
                if (item.Value < 1)
                {
                    Task.Factory.StartNew(() => DeleteWait(item.Key));
                }
                else
                {
                    Task.Factory.StartNew(() => UpdateWait(item.Key, item.Value));
                }
            }
        }
        static void DeleteWait(string epc)
        {
            Thread.Sleep(100);
            int i = 0;
            WaitTags.TryRemove(epc, out i);
        }
        static void UpdateWait(string epc, int val)
        {
            Thread.Sleep(100);
            WaitTags.TryUpdate(epc, val - 1, val);
        }

        private static void LeerSimple()
        {
            Task<bool> TaskConnect = new Task<bool>(() => Antena1.Connect(true, true));
            try
            {
                Console.Write("Conectando...");
                TaskConnect.Start();
                while (!TaskConnect.IsCompleted)
                {
                    Console.Write(".");
                    Thread.Sleep(1000);
                }
                Antena1.Connected = TaskConnect.Result;
                if (Antena1.Connected)
                {
                    Conectado1 = true;
                    Console.WriteLine("Conexión exitosa.");
                    Antena1.Sensors = true;
                    Antena1.Entrance += Antena1_Entrance;
                    Antena1.Exit += Antena1_Exit;
                    Antena1.PassStatusChange += Antena1_PassStatusChange;
                    while (Antena1.Connected && Conectado1 && !tokenSource2.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                    Conectado1 = false;
                }
                else
                {
                    Conectado1 = false;
                }
                Console.WriteLine("Desconectando...");
            }
            catch (Exception)
            {
                Console.WriteLine("La conexión ha fallado.");
                Conectado1 = false;
            }
            finally
            {
                if (Antena1.Connected) DetenerAntena(Antena1);
                Conectado1 = false;
                Console.WriteLine("La antena se ha detenido.");
                TaskConnect.Dispose();
            }
        }

        private static void Antena1_NewReadTag(TagData tag)
        {
            Console.WriteLine("Nuevo Tag en Antena 1: " + tag.EPC);
            if (!DobleTagPass.TryAdd(tag.EPC, ProcessState.E1))
            {
                DobleTagPass.TryUpdate(tag.EPC, ProcessState.S2, ProcessState.S1);
            }
        }
        private static void Antena2_NewReadTag(TagData tag)
        {
            Console.WriteLine("Nuevo Tag en Antena 2: " + tag.EPC);
            if (!DobleTagPass.TryAdd(tag.EPC, ProcessState.S1))
            {
                DobleTagPass.TryUpdate(tag.EPC, ProcessState.E2, ProcessState.E1);
            }
        }
        private static void EraseTags()
        {
            try
            {
                var DobleTagPassCopy = DobleTagPass;
                foreach (var item in DobleTagPassCopy)
                {
                    bool A1 = Antena1.TagList.Find(x => x.EPC == item.Key) != null;
                    bool A2 = Antena2.TagList.Find(y => y.EPC == item.Key) != null;
                    switch (item.Value)
                    {
                        case ProcessState.E2:
                            if (!A1 && !A2)
                            {
                                RestClient.PublishTag(TagServiceURL, new RestClient.RestTag { epc = item.Key, direction = "-1", ip = IP1, rssi = "70", timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                                ProcessState p = ProcessState.E2;
                                DobleTagPass.TryRemove(item.Key, out p);
                                Console.WriteLine("Se registró paso erroneo para: " + item.Key);
                            }
                            else if (A1 && !A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.E1, ProcessState.E2);
                            }
                            else if (!A1 && A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.E3, ProcessState.E2);
                            }
                            break;
                        case ProcessState.E3:
                            if (!A1 && !A2)
                            {
                                RestClient.PublishTag(TagServiceURL, new RestClient.RestTag { epc = item.Key, direction = "1", ip = IP1, rssi = "70", timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                                ProcessState p = ProcessState.E3;
                                DobleTagPass.TryRemove(item.Key, out p);
                                Console.WriteLine("Se registró una entrada para: " + item.Key);
                            }
                            else if (A1 && A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.E2, ProcessState.E3);
                            }
                            else if (A1 && !A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.E1, ProcessState.E3);
                            }
                            break;
                        case ProcessState.S2:
                            if (!A1 && !A2)
                            {
                                RestClient.PublishTag(TagServiceURL, new RestClient.RestTag { epc = item.Key, direction = "-1", ip = IP1, rssi = "70", timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                                ProcessState p = ProcessState.S2;
                                DobleTagPass.TryRemove(item.Key, out p);
                                Console.WriteLine("Se registró paso erroneo para: " + item.Key);
                            }
                            else if (A1 && !A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.S3, ProcessState.S2);
                            }
                            else if (!A1 && A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.S1, ProcessState.S2);
                            }
                            break;
                        case ProcessState.S3:
                            if (!A1 && !A2)
                            {
                                RestClient.PublishTag(TagServiceURL, new RestClient.RestTag { epc = item.Key, direction = "0", ip = IP1, rssi = "70", timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime()) });
                                ProcessState p = ProcessState.S3;
                                DobleTagPass.TryRemove(item.Key, out p);
                                Console.WriteLine("Se registró una salida para: " + item.Key);
                            }
                            else if (A1 && A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.S2, ProcessState.S3);
                            }
                            else if (!A1 && A2)
                            {
                                DobleTagPass.TryUpdate(item.Key, ProcessState.S1, ProcessState.S3);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception)
            {

            }

        }
        public static ProcessState LastState = ProcessState.Inactivo;
        private static void Antena1_PassStatusChange(ProcessState state)
        {
            if (LastState == ProcessState.E3 && state == ProcessState.Inactivo)
            {
                Antena1_Entrance(Antena1.TagList.ToArray());
            }
            if (LastState == ProcessState.S3 && state == ProcessState.Inactivo)
            {
                Antena1_Exit(Antena1.TagList.ToArray());
            }
            Console.WriteLine("Status: " + state.ToString());
            LastState = state;
        }

        private static void DeletePass(string epc)
        {
            Thread.Sleep(100);
            PassType tipo = PassType.None;
            HoldTags.TryRemove(epc, out tipo);
        }
        private enum Movement
        {
            Entrance, Exit
        }
        private static void PublishMovement(Movement movement, string epc, float rssi)
        {
            int v = 0;
            PassType t = PassType.None;

            if (WaitTags.TryGetValue(epc, out v))
            {
                if (v > 0)
                {
                    Console.WriteLine("Movimiento rápido, no publicado");
                    return;
                }
            }
            if (HoldTags.TryGetValue(epc, out t))
            {
                if ((t == PassType.Salida && movement == Movement.Exit) || (t == PassType.Entrada && movement == Movement.Entrance))
                {
                    Console.WriteLine("Movimiento repetido, no publicado");
                    return;
                }
            }
            Task.Factory.StartNew(() => DeletePass(epc));
            WaitTags.TryAdd(epc, 3);
            HoldTags.TryAdd(epc, movement == Movement.Entrance ? PassType.Entrada : PassType.Salida);
            Task.Factory.StartNew(() =>
            {
                RestClient.PublishTag(TagServiceURL, new RestClient.RestTag
                {
                    direction = movement == Movement.Entrance ? "0" : "1",
                    epc = epc,
                    ip = IP1,
                    rssi = rssi.ToString(),
                    timestamp = String.Format("{0:o}", DateTime.Now.ToUniversalTime())
                });
            });
        }
        private static void Antena1_Exit(TagData[] tags)
        {
            if (tags.Length < 1)
            {
                Console.WriteLine("Se detectó una salida de: [Sin Tag]");
                return;
            }
            foreach (var tag in tags)
            {
                if (tag.RSSI < LimiteDeLectura)
                {
                    continue;
                }
                if (tag.EPC != "")
                {
                    try
                    {
                        //PassType type = PassType.None;
                        //if (HoldTags.TryGetValue(tag.EPC, out type))
                        //{
                        //    if (type == PassType.Salida)
                        //    {
                        //        Console.WriteLine("Salida duplicada, no se publicará");
                        //        continue;
                        //    }
                        //    else
                        //    {
                        //        HoldTags.TryUpdate(tag.EPC, PassType.Salida, PassType.Entrada);
                        //        Task.Factory.StartNew(() => DeletePass(tag.EPC));
                        //        Thread.Sleep(10);
                        //        Task.Factory.StartNew(() => DeletePass(tag.EPC));
                        //    }
                        //}
                        //else
                        //{
                        //    HoldTags.TryAdd(tag.EPC, PassType.Salida);
                        //    Thread.Sleep(100);
                        //    HoldTags.TryAdd(tag.EPC, PassType.Salida);
                        //}
                        //int v = 0;
                        //if (WaitTags.TryGetValue(tag.EPC, out v))
                        //{
                        //    Console.WriteLine("Movimiento rápido, no se publicará");
                        //    continue;
                        //}
                        Console.WriteLine("Se detecto una salida de: " + tag.EPC);
                        PublishMovement(Movement.Exit, tag.EPC, tag.RSSI);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Publicación fallida");
                    }

                }
                Antena1.TagList.Clear();
            }
        }

        private static void Antena1_Entrance(TagData[] tags)
        {
            if (tags.Length < 1)
            {
                Console.WriteLine("Se detectó una entrada de: [Sin Tag]");
                return;
            }
            foreach (var tag in tags)
            {
                if (tag.RSSI < LimiteDeLectura)
                {
                    continue;
                }
                if (tag.EPC != "")
                {
                    try
                    {
                        //PassType type = PassType.None;
                        //if (HoldTags.TryGetValue(tag.EPC, out type))
                        //{
                        //    if (type == PassType.Entrada)
                        //    {
                        //        Console.WriteLine("Entrada duplicada, no se publicará");
                        //        continue;
                        //    }
                        //    else
                        //    {
                        //        HoldTags.TryUpdate(tag.EPC, PassType.Entrada, PassType.Salida);
                        //        Task.Factory.StartNew(() => DeletePass(tag.EPC));
                        //        Thread.Sleep(10);
                        //        Task.Factory.StartNew(() => DeletePass(tag.EPC));
                        //    }
                        //}
                        //else
                        //{
                        //    HoldTags.TryAdd(tag.EPC, PassType.Entrada);
                        //    Thread.Sleep(100);
                        //    HoldTags.TryAdd(tag.EPC, PassType.Entrada);
                        //}
                        //int v = 0;
                        //if (WaitTags.TryGetValue(tag.EPC, out v))
                        //{
                        //    continue;
                        //}
                        Console.WriteLine("Se detecto una entrada de: " + tag.EPC);
                        PublishMovement(Movement.Entrance, tag.EPC, tag.RSSI);
                    }
                    catch (Exception) { Console.WriteLine("Publicación fallida"); }
                }
            }

            Antena1.TagList.Clear();
        }

        private static void ConectarDoble()
        {
            Console.Title = String.Format("Antena Doble - IP 1: {0} Potencia 1: {1}, IP 2: {2} Potencia 2: {3}", IP1, Potencia1.ToString(), IP2, Potencia2.ToString());
            Antena1 = new CS203(IP1, Potencia1);
            Antena2 = new CS203(IP2, Potencia2);
            bool salir = false;
            while (!salir)
            {
                salir = false;
                Console.WriteLine("Presiona Esc para salir");
                while (Console.KeyAvailable == false)
                {
                    if (!Conectado1)
                    {
                        Thread.Sleep(1000);
                        Conectado1 = true;

                        tokenSource2 = new CancellationTokenSource();
                        ct = tokenSource2.Token;
                        Task.Factory.StartNew(() =>
                            {
                                ct.ThrowIfCancellationRequested();
                                LeerDoble();
                            }, tokenSource2.Token);
                    }
                    Thread.Sleep(100);
                }
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    salir = true;
                }
            }

        }

        private static void LeerDoble()
        {
            Task<bool> TaskConnect = new Task<bool>(() => Antena1.Connect(true, true));
            Task<bool> TaskConnect2 = new Task<bool>(() => Antena2.Connect(true, true));
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
                if (!Antena1.Connected)
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
                if (!Antena2.Connected)
                {
                    Console.WriteLine("\nNo se pudo conectar la antena: " + IP2);
                    return;
                }
                while (!tokenSource2.IsCancellationRequested && Antena1.Connected && Antena2.Connected)
                {
                    Conectado1 = true;
                    Conectado2 = true;
                    Console.WriteLine("\nConexión exitosa.");
                    Antena1.Sensors = true;
                    Antena1.NewReadTag += Antena1_NewReadTag;
                    Antena2.NewReadTag += Antena2_NewReadTag;
                    while (Antena1.Connected && Conectado1 && !tokenSource2.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                        EraseTags();
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
                
                if (Antena1.Connected) DetenerAntena(Antena1);
                Conectado1 = false;
                if (Antena2.Connected) DetenerAntena(Antena2);
                Conectado2 = false;
                Console.WriteLine("La antena se ha detenido.");
                TaskConnect.Dispose();
                TaskConnect2.Dispose();
            }
        }

        private static void ConectarSimple()
        {
            Console.Title = String.Format("Antena - IP: {0} Potencia: {1}", IP1, Potencia1.ToString());
            Antena1 = new CS203(IP1, Potencia1);
            bool salir = false;
            while (!salir)
            {
                salir = false;
                Console.WriteLine("Presiona Esc para salir");
                Console.WriteLine("Presiona F1 para resetear la antena");
                while (Console.KeyAvailable == false)
                {
                    if (!Conectado1)
                    {
                        Thread.Sleep(1000);
                        Conectado1 = true;

                        tokenSource2 = new CancellationTokenSource();
                        ct = tokenSource2.Token;
                        Task.Factory.StartNew(() =>
                            {
                                ct.ThrowIfCancellationRequested();
                                LeerSimple();
                            }, tokenSource2.Token);
                    }
                    Thread.Sleep(1000);
                }
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.F1)
                {
                    salir = false;
                    tokenSource2.Cancel();
                    DetenerAntena(Antena1);
                    Console.WriteLine("La antena se ha detenido para reconexión.");
                    Console.Write("Esperando para reconexión ");
                    Console.Write("  3  ");
                    Thread.Sleep(1000);
                    Console.Write("  2  ");
                    Thread.Sleep(1000);
                    Console.Write("  1  \n");
                    Thread.Sleep(1000);
                    Console.WriteLine("Reconectando...");
                }
                if (key == ConsoleKey.Escape)
                {
                    salir = true;
                    tokenSource2.Cancel();
                    DetenerAntena(Antena1);
                }
            }

        }
        private static void DetenerAntena(CS203 antena)
        {
            if (antena == null) return;
            Antena1.Entrance -= Antena1_Entrance;
            Antena1.Exit -= Antena1_Exit;
            Antena1.PassStatusChange -= Antena1_PassStatusChange;
            Thread.Sleep(1000);
            antena.Stop();
            Thread.Sleep(1000);
            antena.Stop();
            Thread.Sleep(1000);
            antena.Stop();
            Thread.Sleep(1000);
            Conectado1 = false;
        }
        private static void ConfigurarSimple()
        {
            Console.Clear();
            Console.WriteLine("\t\tAntena Simple");
            Console.Write("Ingrese la IP de la antena: ");
            IP1 = Console.ReadLine();
            bool correct1 = false;
            while (!correct1)
            {
                Console.Write("Ingrese la potencia (0-300): ");
                try
                {
                    Potencia1 = Convert.ToInt32(Console.ReadLine());
                    correct1 = true;
                    if (Potencia1 > 300 || Potencia1 < 0)
                        correct1 = false;
                }
                catch (Exception) { correct1 = false; }
            }
            correct1 = false;
            while (!correct1)
            {
                Console.Write("Ingrese el nivel de respuesta requerida para publicar el tag (0-100): ");
                try
                {
                    LimiteDeLectura = Convert.ToInt32(Console.ReadLine());
                    correct1 = true;
                    if (Potencia1 > 300 || Potencia1 < 0)
                        correct1 = false;
                }
                catch (Exception) { correct1 = false; }
            }
            //Console.Write("¿Desea encender los actuadores? S/N? ");
            //string respactuadores = Console.ReadLine();
            //if (respactuadores.ToLower() == "s")
            //    Actuadores = true;
            //else
            //    Actuadores = false;
        }
        private static void ConfigurarDoble()
        {
            Console.Clear();
            Console.WriteLine("\t\tAntena Doble");
            Console.Write("Ingrese la IP de la primera antena: ");
            IP1 = Console.ReadLine();
            bool correct1 = false;
            while (!correct1)
            {
                Console.Write("Ingrese la potencia (0-300): ");
                try
                {
                    Potencia1 = Convert.ToInt32(Console.ReadLine());
                    correct1 = true;
                    if (Potencia1 > 300 || Potencia1 < 0)
                        correct1 = false;
                }
                catch (Exception) { correct1 = false; }
            }
            //Console.Write("¿Desea encender los actuadores? S/N? ");
            //string respactuadores = Console.ReadLine();
            //if (respactuadores.ToLower() == "s")
            //    Actuadores = true;
            //else
            //    Actuadores = false;
            Console.Write("Ingrese la IP de la segunda antena: ");
            IP2 = Console.ReadLine();
            bool correct2 = false;
            while (!correct2)
            {
                Console.Write("Ingrese la potencia (0-300): ");
                try
                {
                    Potencia2 = Convert.ToInt32(Console.ReadLine());
                    correct2 = true;
                    if (Potencia2 > 300 || Potencia2 < 0)
                        correct2 = false;
                }
                catch (Exception) { correct2 = false; }
            }
        }
        private static void SeleccionarAntena()
        {
            Console.WriteLine("¿Qué tipo de antena desea dar de alta?\n");
            Console.WriteLine("(1) Simple");
            Console.WriteLine("(2) Doble");
            string tipo = "0";
            while (tipo != "1" && tipo != "2")
            {
                Console.Write("Ingrese una de las opciones: (1) ó (2): ");
                tipo = Console.ReadLine();
            }
            if (tipo == "1")
            {
                Tipo = TipoAntena.Simple;
            }
            if (tipo == "2")
            {
                Tipo = TipoAntena.Doble;
            }
        }

        static void Inicializar()
        {
            Console.WriteLine("Middleware - MDW+People v2.1\n");
            Console.Write("Comprobando licencia...");
            Task<bool> taskLicence = Task.Factory.StartNew(() => RestClient.ReadLicense("http://webservice.assetsapp.com/EPCNovag/api/License"));
            int readcount = 0;
            string licenceResult = "No se pudo conectar con el servidor";
            while (!taskLicence.IsCompleted || readcount > 15)
            {
                Console.Write(".");
                Thread.Sleep(1000);
                readcount++;
            }
            if (taskLicence.IsCompleted)
            {
                licenceResult = taskLicence.Result ? "Licencia válida" : "Licencia caducada, contacte a HTK";
            }
            Console.WriteLine(Environment.NewLine + licenceResult);
            Console.WriteLine("");
        }


    }
}
