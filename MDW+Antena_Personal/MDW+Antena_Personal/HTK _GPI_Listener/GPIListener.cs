using CSLibrary;
using CSLibrary.Constants;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HTK__GPI_Listener
{

    public class GPIListener
    {
        public static object Locker = new object();
        public class Reader
        {
            private string ip;
            public string IP
            {
                get
                {
                    lock (Locker)
                    {
                        return IP;
                    }
                }
                set
                {
                    lock (Locker)
                    {
                        ip = value;
                    }
                }
            }
            private bool gpi0;
            public bool GPI0
            {
                get
                {
                    lock (Locker)
                    {
                        return gpi0;
                    }
                }
                set
                {
                    lock (Locker)
                    {
                        gpi0 = value;
                    }
                }
            }
            private bool gpi1;
            public bool GPI1
            {
                get
                {
                    lock (Locker)
                    {
                        return gpi1;
                    }
                }
                set
                {
                    lock (Locker)
                    {
                        gpi1 = value;
                    }
                }
            }
          
            public List<Tag> Tags { get; set; }
            public Reader(string ip)
            {
                IP = ip;
                GPI0 = false;
                GPI1 = false;
                Tags = new List<Tag>();
            }
        }
        public class Tag
        {
            public string IP { get; set; }
            public string EPC { get; set; }
            public string RSSI { get; set; }
            public Tag(string ip, string epc, string rssi)
            {
                IP = ip;
                EPC = epc;
                RSSI = rssi;
            }
        }
        public object MyLocker = new object();

        public static BlackboardProperty<bool> GPI0p = new BlackboardProperty<bool>("GPI0p",false);
        public static BlackboardProperty<bool> GPI1p = new BlackboardProperty<bool>("GPI1p",false);
        public Blackboard GPI;
        public string IP = "";
        public string Server { get; set; }
        public GPIListener(Blackboard gpi, string ip, string server)
        {
            Server = server;
            GPI = gpi;
            IP = ip;
        }
        public void StartReceiving()
        {
            if (HighLevelInterface.StartPollGPIStatus(GPIStatusCallback) == Result.OK)
            {
                Console.WriteLine("StartPollGPIStatus OK!");

            }
            else
            {
                Console.WriteLine("StartPollGPIStatus Failed!");

            }
        }
       
        public bool GPIStatusCallback(string ip, int GPI0, int GPI1)
        {
            new System.Threading.Thread(delegate()
            {
                if (ip == IP)
                {
                    if (GPI0 == 1)
                    {
                        GPI.Set(GPI0p, false);
                    }
                    else if (GPI0 == -1)
                    {
                        GPI.Set(GPI0p, true);

                    }
                    if (GPI1 == 1)
                    {
                        GPI.Set(GPI1p, false);
                    }
                    else if (GPI1 == -1)
                    {
                        GPI.Set(GPI1p, true);
                    }
                }
               
            }).Start();

            return true;
        }
        public void SetInterrupt(string ip)
        {
            if (HighLevelInterface.SetGPI0Interrupt(ip, (GPIOTrigger)3) == Result.OK)
            {
                Console.WriteLine("SetGPI0Interrupt OK!");
            }
            else
            {
                Console.WriteLine("SetGPI0Interrupt Failed!");
            }

            if (HighLevelInterface.SetGPI1Interrupt(ip, (GPIOTrigger)3) == Result.OK)
            {
                Console.WriteLine("SetGPI1Interrupt OK!");
            }
            else
            {
                Console.WriteLine("SetGPI1Interrupt Failed!");

            }
        }
    }

}
