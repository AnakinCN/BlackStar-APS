using System;
using System.Linq;
using BlackStar;
using BlackStar.Model;
using BlackStar.Algorithms;
using BlackStar.USL;
using BlackStar.Rules;

namespace SolutionLoadRun
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Load a BlackStar solution and solve");
            EnvModel.InitializeEnvModel();
            Console.WriteLine(LicensingOP.LicenseInfo);

            USLManagerOP.InitializeUSLOP();
            USLManagerOP.dsUSL.ReadXml("default.usl");
            EnvModel.dsBlackStar.ReadXml("dsBlackStar.bs");
            RuleOP.Execute("算例");

            Console.ReadLine();
            
        }

    }
}
