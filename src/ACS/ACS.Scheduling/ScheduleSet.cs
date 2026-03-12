using Spring.Context.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Scheduling
{
    public class ScheduleSet
    {
        public ScheduleSet()
        {
            Init();
        }
        private void Init()
        {
            try
            {
                XmlApplicationContext ctx = new XmlApplicationContext("spring-objects.xml");
                Console.WriteLine("Spring configuration succeeded, quartz jobs running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.Out.WriteLine("--- Press <return> to quit ---");
                Console.ReadLine();
            }
            Console.Out.WriteLine("--- Press <return> to quit ---");
            Console.ReadLine();
        }
    }
}
