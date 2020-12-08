using System;
using BlackStar;
using BlackStar.Model;

namespace HelloBlackStar
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello BlackStar-APS!");
            EnvModel.InitializeEnvModel();
            Console.WriteLine(LicensingOP.LicenseInfo);
        }
    }
}
