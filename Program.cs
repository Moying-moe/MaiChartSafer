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
            SlideData slide = new SlideData("4V68", 0, 0.5f, 1);
            List<TouchAreaGroup> res = Singleton<SlidePath>.Instance.GetPath(slide);
            foreach (TouchAreaGroup each in res)
            {
                Console.WriteLine(each.ToString());
            }

            Console.ReadKey();
        }
    }
}
