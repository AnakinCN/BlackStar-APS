using System;
using System.Linq;
using BlackStar;
using BlackStar.Model;
using BlackStar.Algorithms;
using BlackStar.USL;

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

            DataSetBlackStar.dtSampleCaseRow samplcase = CreateSampleCase();     //创建算例

            
        }

        private static DataSetBlackStar.dtSampleCaseRow CreateSampleCase()  //创建算例
        {
            DataSetBlackStar.dtSampleCaseRow sample = EnvModel.dsBlackStar.dtSampleCase.NewdtSampleCaseRow();
            sample.主算例 = true;
            sample.算例 = "算例1";
            sample.InitilizeSampleCase();
            EnvModel.dsBlackStar.dtSampleCase.AdddtSampleCaseRow(sample);
            DataSetSampleCase dsSample = new DataSetSampleCase(sample);
            return sample;
        }
    }
}
