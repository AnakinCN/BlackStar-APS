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
            EnvModel.InitializeEnvModel();                              //第1行，初始化型号
            Console.WriteLine(LicensingOP.LicenseInfo);                 

            USLManagerOP.InitializeUSLOP();                             //第2行，初始化USL引擎
            USLManagerOP.dsUSL.ReadXml("default.usl");                  //第3行，读取USL基本配置
            EnvModel.dsBlackStar.ReadXml("dsBlackStar.bs");             //第4行，读取规划方案
            BlackStar.EventAggregators.EventOP.OverAllNotifyEventAggregator.GetEvent<NotifyEvent>().Subscribe(NotifyEventHandler, true);    //第5行，监听规则执行完毕事件
            RuleOP.Execute("算例");                                     //第6行，执行规则
            while (true)
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
