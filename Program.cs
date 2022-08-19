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
            SlideData slide = new SlideData("4-8");
            List<SlideOperation> slideOperations = Singleton<SlideTouchTime>.Instance.GetOperationList(slide);
            foreach (SlideOperation operation in slideOperations)
            {
                Console.WriteLine(operation.ToString());
            }

            Console.ReadKey();
        }
    }
}
