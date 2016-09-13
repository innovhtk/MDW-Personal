using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HTKRestClient;

namespace HTK__GPI_Listener
{
    public static class RestPublisher
    {
        public static int WaitTime = 10;
        private static string url = "";
        public static string URL
        {
            get
            {
                return url;
            }
            set
            {
                if (value == "")
                {
                    url = "";
                    return;
                }
                if (value.Contains("http"))
                {
                    url = value;
                    return;
                }
                url = "http://" + value + "/api/";
            }
        }
        internal static void Publish(string ip, string epc, float rssi, string timestamp, string direction, string url)
        {
            if (String.IsNullOrEmpty(url))
            {
                //URL = "http://192.168.1.219/api/";
                Console.WriteLine("RestPublisher: URL nula");
                return;
            }
            if (url == "")
            {
                url = "";
                return;
            }
            string server = url;
            if (!url.Contains("http"))
            {
                server = "http://" + url + "/api/";
            }
            Console.WriteLine("Estoy publicando en :" + RestPublisher.URL);
            RestClient.TiempoSalidaDeEmergencia = WaitTime;
            Console.WriteLine(String.Format("{0}\t{1}\t{2}\t\t{3}\t{4}",timestamp,ip,direction == "0" ? "Entrada":"Salida",epc,rssi.ToString()));
            Task.Factory.StartNew(() =>
            {
                RestClient.PublishTag(server, Program.ipDB, new RestClient.RestTag
                {
                    direction = direction,
                    epc = epc,
                    ip = ip,
                    rssi = rssi.ToString(),
                    timestamp = timestamp
                });
            });
        }
    }
}
