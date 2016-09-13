using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MDW_Antena_Controller
{
    public partial class Form1 : Form
    {
        public static string[] nombre = new string[40];
        public Form1()
        {
            InitializeComponent();
        }
        public static void SearchAndConnect(string ip, string power, string rssi, string url, string waitTime, string simplePass, string ipDB)
        {
            Process thisProc = Process.GetCurrentProcess();
            if (IsProcessOpen(ip) == false)
            {
                OpenProcess(ip, power, rssi, url, waitTime, simplePass, ipDB);
            }
           
        }
        public static void SearchAndConnectEmergencia(string ip, string power, string rssi, string url, string waitTime, string ipDB)
        {
            Process thisProc = Process.GetCurrentProcess();
            if (IsProcessOpen(ip) == false)
            {
                OpenProcessEmergencia(ip, power, rssi, url, waitTime, ipDB);
            }

        }
         public static void SearchAndConnectPrincipal(string ip1, string power1, string rssi1, string ip2, string power2, string rssi2, string url, string inin, string outout, string inout, string outin, string ipDB)
        {
            Process thisProc = Process.GetCurrentProcess();
            if (IsProcessOpen(ip1) == false)
            {
                OpenPrincipalProcess(ip1,power1, rssi1, ip2, power2, rssi2, url, inin, outout, inout, outin, ipDB);
            }
          
        }
       public static void SearchAndConnectDoble(string ip1, string power1, string rssi1, string ip2, string power2, string rssi2, string url, string wait, string ipDB)
        {
            Process thisProc = Process.GetCurrentProcess();
            if (IsProcessOpen(ip1) == false)
            {
                OpenDobleProcess(ip1,power1, rssi1, ip2, power2, rssi2, url, wait, ipDB);
            }
          
        }
        public static void OpenProcess(string currentAntenna, string power,string rssi, string url, string waitTime, string simplePass, string ipDB)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Desconectada\t" + currentAntenna);
            string app = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\\MDW+AntenaSimple.exe";
            //Process.Start("antena", app);

            Process antena = new Process();
            antena.StartInfo.FileName = app;
            antena.StartInfo.Arguments = String.Format(" {0} {1} {2} {3} {4} {5} {6} {7}", new string[] { currentAntenna,power, rssi, url, url, waitTime, simplePass, ipDB  });
            antena.Start();
        }
        public static void OpenProcessEmergencia(string currentAntenna, string power, string rssi, string url, string waitTime, string ipDB)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Desconectada\t" + currentAntenna);
            string app = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\\HTK _GPI_Listener.exe";
            //Process.Start("antena", app);

            Process antena = new Process();
            antena.StartInfo.FileName = app;
            antena.StartInfo.Arguments = String.Format(" {0} {1} {2} {3} {4} {5}", new string[] { currentAntenna, power, rssi, url, waitTime, ipDB });
            antena.Start();
        }
        public static void OpenPrincipalProcess(string currentAntenna, string power1, string rssi1, string currentAntenna2, string power2, string rssi2, string url, string inin, string outout, string inout, string outin, string ipDB)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Desconectada\t" + currentAntenna);
            string app = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\\MDW+AntenaPrincipalDoble.exe";
            //Process.Start("antena", app);

            Process antena = new Process();
            antena.StartInfo.FileName = app;
            antena.StartInfo.Arguments = String.Format(" {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12}", new string[] { currentAntenna, power1, rssi1, currentAntenna2, power2, rssi2, url, url, inin, outout, inout, outin, ipDB });
            antena.Start();
        }
          public static void OpenDobleProcess(string currentAntenna, string power1, string rssi1, string currentAntenna2, string power2, string rssi2, string url, string wait, string ipDB)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Desconectada\t" + currentAntenna);
            string app = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\\MDW+AntenaDoble.exe";
            //Process.Start("antena", app);

            Process antena = new Process();
            antena.StartInfo.FileName = app;
            antena.StartInfo.Arguments = String.Format(" {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", new string[] { currentAntenna, power1, rssi1, currentAntenna2, power2, rssi2, url, url, wait, ipDB });
            antena.Start();
        }
      
        public static bool IsProcessOpen(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.MainWindowTitle.Contains(name))
                {
                    //Console.WriteLine(clsProcess.ProcessName);
                    return true;
                }
            }
            return false;
        }
        public static bool CloseProcess(string name)
        {
            try
            {
                foreach (Process clsProcess in Process.GetProcesses())
                {
                    if (clsProcess.MainWindowTitle.Contains(name))
                    {
                        //Console.WriteLine(clsProcess.ProcessName);
                        clsProcess.Kill();
                        //return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => ConstantCheck());
           
        }
        public void ConstantCheck()
        {
            while (true)
            {
                try
                {
                    if (cbConectarPrincipal.Checked) { SearchAndConnect(tbIP1.Text, tbPotencia1.Text, tbRssi1.Text, tbWSTags.Text, tbWait1.Text, "simplepass", tbIPDB.Text); }
                    else if (cbConectarPrincipalDoble.Checked) { SearchAndConnectPrincipal(tbIP8.Text, tbPotencia8.Text, tbRssi8.Text, tbIP9.Text, tbPotencia9.Text, tbRssi9.Text, tbWSGPIO.Text, tbInIn.Text, tbOutOut.Text, tbInOut.Text, tbOutIn.Text, tbIPDB.Text); };
                    if (cbConectarEmergencia.Checked) { SearchAndConnectEmergencia(tbIP2.Text, tbPotencia2.Text, tbRssi2.Text, tbWSTags.Text, tbWait2.Text, tbIPDB.Text); }
                    if (cbComedorSimple.Checked) { SearchAndConnect(tbIP3.Text, tbPotencia3.Text, tbRssi3.Text, tbWSTags.Text, tbWait3.Text, "simplepass", tbIPDB.Text); }
                    if (cbComedorDoble.Checked) { SearchAndConnectDoble(tbIP6.Text, tbPotencia6.Text, tbRssi6.Text, tbIP7.Text, tbPotencia7.Text, tbRssi7.Text, tbWSGPIO.Text, tbWait4.Text, tbIPDB.Text); }
                    if (cbConectarExclusas.Checked) { SearchAndConnectDoble(tbIP4.Text, tbPotencia4.Text, tbRssi4.Text, tbIP5.Text, tbPotencia5.Text, tbRssi5.Text, tbWSGPIO.Text, tbWait5.Text, tbIPDB.Text); }

                }
                catch (Exception)
                {
                }
               
                Thread.Sleep(1000);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseProcess(tbIP1.Text);
            CloseProcess(tbIP2.Text);
            CloseProcess(tbIP3.Text);
            CloseProcess(tbIP4.Text);
            CloseProcess(tbIP5.Text);
            CloseProcess(tbIP6.Text);
            CloseProcess(tbIP7.Text);
        }





    }
}
