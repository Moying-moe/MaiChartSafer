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
            Singleton<Simulator>.Instance.LoadChartFromMajdata(@"D:\maimaifanmade\测试\majdata.json");

            Singleton<Simulator>.Instance.Simulate();

            Console.WriteLine(Singleton<Simulator>.Instance.GetSimulateResult(ResultLevel.Critical));

            Console.WriteLine("Wait Any Key to Exit.");
            Console.ReadKey();
        }
    }
}
