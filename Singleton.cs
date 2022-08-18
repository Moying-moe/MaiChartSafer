using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaiChartSafer
{
    public class Singleton<T> where T : class, new()
    {
        public static T Instance
        {
            get
            {
                return Singleton<T>._instance;
            }
        }

        private static readonly T _instance = Activator.CreateInstance<T>();
    }
}
