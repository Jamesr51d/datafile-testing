using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ddtest
{
    public class UnitTest
    {

        public UnitTest()
        {

        }

        public  void RunTests(string whichTest)
        {
            switch (whichTest)
            {
                case "1":
                    PropertyDetection();
                    break;
                case "2":
                    PerformanceDetection();
                    break;
                case "3":
                    GeneralDetectionTest();
                    break;

            }
        }


        public void PropertyDetection()
        {

        }

        public void PerformanceDetection()
        {

        }

        public void GeneralDetectionTest()
        {

        }

    }
}
