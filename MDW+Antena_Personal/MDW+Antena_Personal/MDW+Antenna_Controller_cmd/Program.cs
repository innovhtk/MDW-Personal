using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MDW_Antenna_Controller_cmd
{
    class Program
    {
        public static string[] nombre = new string[40];
        static void Main(string[] args)
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            nombre[1] = "192.168.25.1\tEntrada Principal";

            while (true)
            {
                Console.SetCursorPosition(0, 0);
                //Console.Clear();
                SearchAndConnect(1);
                //SearchAndConnect(203);
                int minutos = 1;
                int segundos = minutos * 60;
                int milisegundos = segundos * 1000;
                Thread.Sleep(milisegundos);
            }

            
        }

        public static void SearchAndConnect(int i)
        {
            Process thisProc = Process.GetCurrentProcess();
            string currentAntenna = i.ToString();
            if (IsProcessOpen("192.168.1." + currentAntenna + " Conectada") == false)
            {
                OpenProcess(currentAntenna);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Conectada\t" + nombre[i]);
            }
        }
        public static void OpenProcess(string currentAntenna)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Desconectada\t" + nombre[Convert.ToInt32(currentAntenna)]);
            string app = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "\\MDW+AntenaSimple.exe";

            Process antena = new Process();
            antena.StartInfo.FileName = app;
         
            antena.StartInfo.Arguments = String.Format(" {0} {1} {2} {3} {4} {5} {6}",new string[] { 
                    "192.168.1." + currentAntenna.ToString(), 
                    "200", 
                    "40",
                    "http://192.168.1.219/api/",
                    "http://192.168.1.219/api/",
                    "3",
                    "simplepass"
                });
            antena.Start();
            Thread.Sleep(5000);
        }
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                Console.WriteLine("\n\nCerrando....\n");
                CloseProcess(" Conectada");
            }
            return false;
        }
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
        // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

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

    }
}
