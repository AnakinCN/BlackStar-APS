using System;
using BlackStar.Model;
using BlackStar.USL;
using BlackStar.Rules;
using BlackStar.EventAggregators;
using System.Diagnostics;

namespace MaxUserate
{
    class Program
    {
        static Stopwatch sw = new Stopwatch();
        static void Main(string[] args)
        {
            Console.WriteLine("Load a BlackStar solution and solve");
            EnvModel.InitializeEnvModel();                              //Initialize a model
            Console.WriteLine(LicensingOP.LicenseInfo);

            USLManagerOP.InitializeUSLOP();                             //Initialize the USL engine
            USLManagerOP.dsUSL.ReadXml("dsBlackStar.usl");              //load the usl definitions
            EnvModel.dsBlackStar.ReadXml("dsBlackStar.bs");             //Load the scheduling definitions


            EnvModel.dsBlackStar.dtSampleCase.GetMainSampleCase().InitilizeSampleCase();
            EnvModel.dsBlackStar.dtSampleCase.GetMainSampleCase().dsSampleCase.ReadXml("dsSampleCase.ds");  //Load the SampleCase
            EnvModel.dsBlackStar.dtSampleCase.GetMainSampleCase().dsSnap.ReadXml("dsSnap.ds");              //Load the snapshot of Samplecase

            EventOP.OverAllNotifyEventAggregator.GetEvent<NotifyEvent>().Subscribe(NotifyEventHandler, true);    //Listen to "RuleExecuteComplete" event

            sw.Start();
            RuleOP.Execute("快照");                                     //Execute the rules
            while (true)
                Console.ReadLine();
        }

        private static void NotifyEventHandler(string NotifyMessage)
        {
            switch (NotifyMessage)
            {
                case "RuleExecuteComplete":     //this message is sent when rule execution complete
                    sw.Stop();
                    Console.WriteLine(sw.Elapsed);
                    //PrintResult();
                    break;
            }
        }

        private static void PrintResult()   //print results
        {
            DataSetBlackStar.dtSampleCaseRow samplcase = EnvModel.dsBlackStar.dtSampleCase.Rows[0] as DataSetBlackStar.dtSampleCaseRow;
            Console.WriteLine("事件结果：");
            foreach (DataSetSampleCase.dtEventRow eventrow in samplcase.dsSampleCase.dtEvent.Rows)
            {
                Console.WriteLine($"{eventrow.事件名称} {eventrow.事件代号} {eventrow.开始} {eventrow.结束}");
            }
            Console.WriteLine("规则日志：");
            foreach (DataSetSampleCase.dtSampleCaseLogRow log in samplcase.dsSampleCase.dtSampleCaseLog.Rows)
            {
                Console.WriteLine($"{log.时间} {log.消息} ");
            }
        }
    }
}
