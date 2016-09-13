using CSLibrary;
using CSLibrary.Constants;
using CSLibrary.Structures;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HTK__GPI_Listener
{

    static class Reader
    {
        public static HighLevelInterface ReaderXP = new HighLevelInterface();
        public static string applicationSettings = "application.config";
        public static appSettings appSetting = new appSettings();
        public static string SerialNumber = String.Empty;
        public static List<Tag> Tags = new List<Tag>();
        public static System.Timers.Timer EraseTimer = new System.Timers.Timer(1000);
        private static Thread reset;
        public static string IP { get; set; }
        public static int Power { get; set; }
        public static string Server { get; set; }

        public class Tag
        {
            public string IP { get; set; }
            public string EPC { get; set; }
            public float RSSI { get; set; }
            public int EraseTime { get; set; }

            public Tag(string ip, string epc, float rssi)
            {
                IP = ip;
                EPC = epc;
                RSSI = rssi;
                EraseTime = 2;
            }
        }

            public static DoorKeeper keeper = new DoorKeeper();
        public static void ConnectAndWatch(string ip, int power, string server)
        {
            Server = server;
            IP = ip;
            Power = power;
            Connect(ip, power);
            Start();
            EraseTimer.Elapsed += EraseTimer_Elapsed;
            EraseTimer.Start();
            keeper.Watch(ip, server);
            
        }
        public static void Watch(string ip, string server)
        {
            Start();
            EraseTimer.Elapsed += EraseTimer_Elapsed;
            EraseTimer.Start();
            keeper.Watch(ip, server);
        }

        private static void EraseTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            for (int i = 0; i < Tags.Count; i++)
                Tags[0].EraseTime--;
            Tags.RemoveAll(t => t.EraseTime < 1);
        }

        public static void Connect(string ip, int power)
        {
            Console.WriteLine("Conectando " + ip + " ...");
            CSLibrary.Constants.Result ret = CSLibrary.Constants.Result.OK;
            int time = Environment.TickCount;
            if ((ret = ReaderXP.Connect(ip, 20000)) != CSLibrary.Constants.Result.OK)
            {
                ReaderXP.Disconnect();
                Console.WriteLine(String.Format("StartupReader Failed{0}", ret));
                Thread.Sleep(2000);
                Environment.Exit(0);
            }
            if (ReaderXP.SetPowerLevel(Convert.ToUInt32(power)) != Result.OK)
            {
                Console.WriteLine("Error al cambiar la potencia!!");
            }
            System.Diagnostics.Trace.WriteLine(string.Format("Connect time = {0}", Environment.TickCount - time));

            string MyDocumentFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\CSLReader";

            try
            {
                System.IO.Directory.CreateDirectory(MyDocumentFolder);
            }
            catch (Exception ) { }

            if ((SerialNumber = ReaderXP.GetPCBAssemblyCode()) == null)
            {
                Console.WriteLine(String.Format("GetPCBAssemblyCode Failed"));
            }

            if (!System.IO.File.Exists(MyDocumentFolder + "\\" + SerialNumber + ".cfg"))
            {
                LoadDefaultSettings();
            }
            else
            {
                LoadDefaultSettings();
                //LoadSettings();
            }

            //Open MainForm and EnableVisualStyles
            //----------------Original------------
            //Application.Run(new MenuForm()); 



            //----------------Abrir Inventario con GPIO--------------
            if (appSetting.tagGroup.selected != CSLibrary.Constants.Selected.ALL)
                Console.WriteLine("Warning : MASK IS SET !!!");

            Console.WriteLine("Conectado");
        }
        private static void AttachCallback(bool en)
        {
            if (en)
            {
                ReaderXP.OnStateChanged += new EventHandler<CSLibrary.Events.OnStateChangedEventArgs>(ReaderXP_StateChangedEvent);
                ReaderXP.OnAsyncCallback += new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(ReaderXP_TagInventoryEvent);
            }
            else
            {
                ReaderXP.OnAsyncCallback -= new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(ReaderXP_TagInventoryEvent);
                ReaderXP.OnStateChanged -= new EventHandler<CSLibrary.Events.OnStateChangedEventArgs>(ReaderXP_StateChangedEvent);
            }
        }
        public static void AbortReset()
        {
            if (reset != null && reset.IsAlive)
            {
                reset.Abort();
            }
        }
        //private static bool LedOn = false;
        private static void ReaderXP_StateChangedEvent(object sender, CSLibrary.Events.OnStateChangedEventArgs e)
        {
            new System.Threading.Thread(delegate()
            {
                switch (e.state)
                {
                    case RFState.IDLE:
                        break;
                    case RFState.BUSY:
                        break;
                    case RFState.RESET:
                        Console.WriteLine("\n\nLa antena entró en reset\n\n");

                        //Use other thread to create progress
                        reset = new Thread(new ThreadStart(Reset));
                        reset.Start();

                        break;
                    case RFState.ABORT:
                        //ControlPanelForm.EnablePannel(false);
                        Environment.Exit(0);
                        break;

                    case RFState.ANT_CYCLE_END:
                        //ReaderXP.SetGPO0Async(LedOn);
                        //LedOn = !LedOn;
                        break;
                }
            }).Start();
        }

        private static void ReaderXP_TagInventoryEvent(object sender, CSLibrary.Events.OnAsyncCallbackEventArgs e)
        {

            new System.Threading.Thread(delegate()
            {
                // Do your work here
                // UI refresh and data processing on other Thread
                // Notes :  blocking here will cause problem
                //          Please use asyn call or separate thread to refresh UI
                if (!e.info.crcInvalid)
                {
                    if ((!appSetting.EnableRssiFilter) ||
                        (appSetting.EnableRssiFilter && appSetting.RssiFilterThreshold < e.info.rssi))
                    {
                        //Interlocked.Increment(ref totaltags);
                        TagCallbackInfo data = e.info;
                        //Console.WriteLine(data.epc.ToString());
                        try
                        {
                            int foundIndex = Tags.FindIndex(delegate(Tag tag) { return tag.EPC == data.epc.ToString(); });
                            if (foundIndex >= 0)
                            {

                                Tags[foundIndex].RSSI = data.rssi;
                                Tags[foundIndex].EraseTime = 2;
                            }
                            else
                            {
                                Tag tag = new Tag(IP, data.epc.ToString(), data.rssi);
                                Tags.Add(tag);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }).Start();
        }

        public static void Start()
        {
            AttachCallback(true);
            if (ReaderXP.State == RFState.IDLE)
            {
                ReaderXP.SetOperationMode(RadioOperationMode.CONTINUOUS);
                ReaderXP.SetTagGroup(appSetting.tagGroup);
                ReaderXP.SetSingulationAlgorithmParms(appSetting.Singulation, appSetting.SingulationAlg);
                //Do Setup on SettingForm
                ReaderXP.Options.TagRanging.flags = SelectFlags.ZERO;

                ReaderXP.Options.TagRanging.multibanks = 0;


                ReaderXP.Options.TagRanging.QTMode = false; // reset to default
                ReaderXP.Options.TagRanging.accessPassword = 0x0; // reset to default

                ReaderXP.StartOperation(Operation.TAG_RANGING, false);
            }
        }
        public static void StartOnce()
        {
            if (ReaderXP.State == RFState.IDLE)
            {
                ReaderXP.SetOperationMode(RadioOperationMode.NONCONTINUOUS);
                ReaderXP.SetTagGroup(appSetting.tagGroup);
                ReaderXP.SetSingulationAlgorithmParms(appSetting.Singulation, appSetting.SingulationAlg);
                //Do Setup on SettingForm
                ReaderXP.Options.TagRanging.flags = SelectFlags.ZERO;
                ReaderXP.Options.TagRanging.QTMode = false; // reset to default
                ReaderXP.Options.TagRanging.accessPassword = 0x0; // reset to default
                ReaderXP.StartOperation(Operation.TAG_RANGING, false);
            }
        }

        public static void Stop()
        {
            if (ReaderXP.State == RFState.BUSY)
            {
                ReaderXP.StopOperation(true);
            }
        }

        private static void Reset()
        {
            Result rc = Result.OK;
        RETRY:
            //Reset Reader first, it will shutdown current reader and restart reader
            //It will also reconfig back previous operation
            if ((rc = ReaderXP.Reconnect(30000)) == Result.OK)
            {
                Console.WriteLine("\n\nLa antena se ha reconectado\n\n");
                int retry = 1000;
                //Start inventory
                ReaderXP.SetOperationMode(RadioOperationMode.CONTINUOUS);
                while (ReaderXP.StartOperation(Operation.TAG_RANGING, false) != Result.OK)
                {
                    if (retry-- == 0)
                        break;
                }
                Watch(IP, Server);
                Console.WriteLine("\n\nLa antena se ha reconfigurado\n\n");
            }
            else
            {
                //if (ShowMsg(String.Format("ResetReader fail rc = {0}. Do you want to retry?", rc)) == DialogResult.Yes)
                //Console.WriteLine(String.Format("ResetReader fail rc = {0}. Do you want to retry?", rc));
                Console.WriteLine("Reconectando..");
                goto RETRY;
            }
        }
        public static void Clear()
        {
            //if (this.InvokeRequired)
            //{
            //    Invoke(new MethodInvoker(Clear));
            //    return;
            //}
            //InventoryListItems.Clear();
            //m_sortListView.Items.Clear();
        }
        private static bool LoadDefaultSettings()
        {
            uint power = 0, linkProfile = 0;
            SingulationAlgorithm sing = SingulationAlgorithm.UNKNOWN;
            appSetting.SerialNum = SerialNumber;

            if (ReaderXP.GetPowerLevel(ref power) != Result.OK)
            {
                Console.WriteLine(String.Format("SetPowerLevel rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return false;
            }
            appSetting.Power = power;

            if (ReaderXP.GetCurrentLinkProfile(ref linkProfile) != Result.OK)
            {
                Console.WriteLine(String.Format("SetCurrentLinkProfile rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return false;
            }
            appSetting.Link_profile = linkProfile;
            if (appSetting.FixedChannel = ReaderXP.IsFixedChannel)
            {
                appSetting.Region = ReaderXP.SelectedRegionCode;
                appSetting.Channel_number = ReaderXP.SelectedChannel;
                appSetting.Lbt = ReaderXP.LBT_ON == LBT.ON;
            }
            else
            {
                appSetting.Region = ReaderXP.SelectedRegionCode;
            }

            if (ReaderXP.GetCurrentSingulationAlgorithm(ref sing) != Result.OK)
            {
                Console.WriteLine(String.Format("GetCurrentSingulationAlgorithm rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return false;
            }
            appSetting.Singulation = sing;

            if (ReaderXP.GetSingulationAlgorithmParms(appSetting.Singulation, appSetting.SingulationAlg) != Result.OK)
            {
                Console.WriteLine(String.Format("GetCurrentSingulationAlgorithm rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return false;
            }

            appSetting.AntennaList = AntennaList.DEFAULT_ANTENNA_LIST;
            return true;
        }

        public static void LoadSettings()
        {
            appSetting.AntennaList = AntennaList.DEFAULT_ANTENNA_LIST;
            appSetting.AntennaList.Clear();
            appSetting = appSetting.Load();

            //Previous save config not match
            if (appSetting.SerialNum != SerialNumber)
                return;

            if (ReaderXP.SetCurrentLinkProfile(appSetting.Link_profile) != Result.OK)
            {
                Console.WriteLine(String.Format("SetCurrentLinkProfile rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return;
            }

            if (appSetting.FixedChannel)
            {
                if (appSetting.FreqAgile == false)
                {
                    if (ReaderXP.SetFixedChannel(appSetting.Region, appSetting.Channel_number, appSetting.Lbt ? LBT.ON : LBT.OFF) != Result.OK)
                    {
                        Console.WriteLine(String.Format("SetFixedChannel rc = {0}", ReaderXP.LastResultCode));
                        Environment.Exit(0);
                        return;
                    }
                }
                else
                {
                    if (ReaderXP.SetAgileChannels(appSetting.Region) != Result.OK)
                    {
                        Console.WriteLine(String.Format("SetAgileChannel rc = {0}", ReaderXP.LastResultCode));
                        Environment.Exit(0);
                        return;
                    }
                }
            }
            else
            {
                if (ReaderXP.SetHoppingChannels(appSetting.Region) != Result.OK)
                {
                    Console.WriteLine(String.Format("SetHoppingChannels rc = {0}", ReaderXP.LastResultCode));
                    Environment.Exit(0);
                    return;
                }
            }
            if (ReaderXP.SetSingulationAlgorithmParms(appSetting.Singulation, appSetting.SingulationAlg) != Result.OK)
            {
                Console.WriteLine(String.Format("SetSingulationAlgorithmParms rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return;
            }

            if (appSetting.AntennaList == null)
                ReaderXP.AntennaList = AntennaList.DEFAULT_ANTENNA_LIST;
            else
                ReaderXP.AntennaList.Copy(appSetting.AntennaList);

            if (appSetting.AntennaList.Store(ReaderXP) != Result.OK)
            {
                Console.WriteLine(String.Format("SetAntennaList rc = {0}", ReaderXP.LastResultCode));
                Environment.Exit(0);
                return;
            }

            if (appSetting.AntennaSequenceMode == AntennaSequenceMode.SEQUENCE ||
                appSetting.AntennaSequenceMode == AntennaSequenceMode.SEQUENCE_SMART_CHECK)
            {
                ReaderXP.AntennaSequenceSize = appSetting.AntennaSequenceSize;
                ReaderXP.AntennaSequenceMode = appSetting.AntennaSequenceMode;
                Array.Copy(appSetting.AntennaPortSequence, 0, ReaderXP.AntennaPortSequence, 0, appSetting.AntennaPortSequence.Length);
                if (ReaderXP.SetAntennaSequence(ReaderXP.AntennaPortSequence, ReaderXP.AntennaSequenceSize, ReaderXP.AntennaSequenceMode) != Result.OK)
                {
                    Console.WriteLine(String.Format("SetAntennaSequence rc = {0}", ReaderXP.LastResultCode));
                    Environment.Exit(0);
                    return;
                }
            }
            else
            {
                ReaderXP.AntennaSequenceSize = 0;
                ReaderXP.SetAntennaSequence((int)ReaderXP.AntennaSequenceSize);
            }
        }

    }
}
