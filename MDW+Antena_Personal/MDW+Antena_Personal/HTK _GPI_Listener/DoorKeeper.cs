using System;
using System.Collections.Generic;
using System.Threading;

namespace HTK__GPI_Listener
{
    public class DoorKeeper
    {
        public static BlackboardProperty<bool> Passing = new BlackboardProperty<bool>("Passing", false);
        public string Server { get; set; }
        public string IP { get; set; }
        public void Watch(string IP, string server)
        {
            this.Server = server;
            this.IP = IP;
            Blackboard sensores = new Blackboard();
            GPIListener listener = new GPIListener(sensores, IP, server);
            listener.SetInterrupt(IP);
            listener.StartReceiving();

            sensores.Set<bool>(Passing, false);
            //Console.WriteLine("GPI0: 0" + " GPI1: 0");
            //Console.WriteLine("");
            while (true)
            {
                var gpi0 = sensores.Get<bool>(GPIListener.GPI0p);
                var gpi1 = sensores.Get<bool>(GPIListener.GPI1p);
                var passing = sensores.Get<bool>(Passing);
                //Console.SetCursorPosition(0, Console.CursorTop - 2);
                string g0 = gpi0 ? "1" : "0";
                string g1 = gpi1 ? "1" : "0";
                //Console.WriteLine("GPI0: " + g0 + " GPI1: " + g1);
                //string resultado = "\t\t";
                //if (passing)
                //{
                //    resultado = "";
                //}
                if (g0 == "1" && !passing)
                {
                    if (g1 == "1")
                    {
                        //resultado = "Salida\t";
                        sensores.Set<bool>(Passing, true);
                        Console.WriteLine("Salida\n");
                        if (Reader.Tags.Count > 0)
                        {
                            Green("0");//original: Green("1");
                        }
                        else
                        {
                            Red();
                        }
                    }
                    else
                    {
                        //resultado = "Entrada\t";
                        sensores.Set<bool>(Passing, true);
                        Console.WriteLine("Entrada\n");
                        if (Reader.Tags.Count > 0)
                        {
                            Green("1");//original: Green("1");
                        }
                        else
                        {
                            Red();
                        }
                    }
                }
                if (g0 == "0" && g1 == "0")
                {
                    sensores.Set<bool>(Passing, false);
                }
                //Console.WriteLine(resultado);
                Thread.Sleep(10);
            }
        }
        private void Green(string direction)
        {
            var list = new List<Reader.Tag>(Reader.Tags);
            foreach (var item in list)
            {
                RestPublisher.Publish(item.IP, item.EPC, item.RSSI, String.Format("{0:o}", DateTime.Now.ToUniversalTime()), direction, Server);
            }
            if (IP == "192.168.1.231") { return; }
            Reader.ReaderXP.SetGPO0Async(true);
            Thread.Sleep(200);
            Reader.ReaderXP.SetGPO0Async(false);
        }

        private void Red()
        {
            if (IP == "192.168.1.231") { return; }
            Reader.ReaderXP.SetGPO1Async(true);
            Thread.Sleep(200);
            Reader.ReaderXP.SetGPO1Async(false);
        }
    }
}
