using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    class Program
    {
        static void Main(string[] args)
        {
            Singleton<Simulator>.Instance.Init();
            Singleton<Simulator>.Instance.LoadChartFromMajdata(@"D:\maimaifanmade\新谱\ARTEMIS\majdata.json");

            Console.WriteLine("Wait Any Key to Exit.");
            Console.ReadKey();
        }
    }
}
