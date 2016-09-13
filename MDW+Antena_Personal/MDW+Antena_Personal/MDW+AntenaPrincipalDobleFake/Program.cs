using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDW_AntenaPrincipalDobleFake
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                Console.WriteLine(arg);
            }
            Console.WriteLine(new string('-', 20));
            Console.Read();
        }
    }
}
