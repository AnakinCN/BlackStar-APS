using System;
using BlackStar.Model;
using BlackStar.USL;
using BlackStar.Rules;
using BlackStar.EventAggregators;

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
            BlackStar.EventAggregators.EventOP.OverAllNotifyEventAggregator.GetEvent<NotifyEvent>().Subscribe(NotifyEventHandler, true);    //监听规则执行完毕事件
            RuleOP.Execute("算例");
            while(true)
                Console.ReadLine();
        }

        private static void NotifyEventHandler(string NotifyMessage)
        {
            switch (NotifyMessage)
            {
                case "RuleExecuteComplete":     //当规则执行完毕会发出该消息
                    PrintResult();
                    break;
            }
        }

        private static void PrintResult()   //输出结果
        {
            DataSetBlackStar.dtSampleCaseRow samplcase = EnvModel.dsBlackStar.dtSampleCase.Rows[0] as DataSetBlackStar.dtSampleCaseRow;
            Console.WriteLine("事件结果：");
            foreach (DataSetSampleCase.dtEventRow eventrow in samplcase.dsSampleCase.dtEvent.Rows)
            {
                Console.WriteLine($"{eventrow.事件名称} {eventrow.事件代号} {eventrow.开始} {eventrow.结束}");
            }
        }
    }
}
