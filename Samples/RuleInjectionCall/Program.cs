using System;
using BlackStar.Model;
using BlackStar.USL;
using BlackStar.Rules;
using BlackStar.EventAggregators;

namespace RuleInjectionCall
{
    class Program
    {
        //将编译好的自定义规则复制到应用目录下，启动程序后，会自动检索并加载，随后即可同内部规则一样使用。
        static void Main(string[] args)
        {
            Console.WriteLine("读入并运行注入规则");
            EnvModel.InitializeEnvModel();                  //初始化型号
            Console.WriteLine(LicensingOP.LicenseInfo);

            USLManagerOP.InitializeUSLOP();                 //初始化USL引擎
            USLManagerOP.dsUSL.ReadXml("dsBlackStar.usl");  //读取USL基本配置
            EnvModel.dsBlackStar.ReadXml("dsBlackStar.bs"); //读取规划方案
            CallRule();                                     //调用注入的规则
            EventOP.OverAllNotifyEventAggregator.GetEvent<NotifyEvent>().Subscribe(NotifyEventHandler, true);   //监听规则执行完毕事件
            RuleOP.Execute("算例");                           //执行规则
            while (true)
                Console.ReadLine();
        }

        private static void CallRule()
        {
            //规则
            Guid id = Guid.NewGuid();
            DataSetBlackStar.dtRuleRow rule = EnvModel.dsBlackStar.dtRule.NewdtRuleRow();
            rule.规则ID = id;
            rule.分组 = "默认";
            rule.规则 = "MyLib：MyRule";       //调用规则炒鸡简单吧？
            rule.默认参数 = "显示消息";        //你的规则默认参数是什么，就填什么
            rule.取值 = "这是一条注入规则，可以任意加入算法，操作算例";     //默认参数的取值
            rule.有效 = true;
            EnvModel.dsBlackStar.dtRule.AdddtRuleRow(rule);

            //规则参数，你的规则有几个参数，就设置几个
            rule.SetPara("显示消息", "这是一条注入规则，可以任意加入算法，操作算例");
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
                Console.WriteLine($"{eventrow.事件名称} {eventrow.事件代号} {eventrow.开始} {eventrow.结束} {eventrow.明细}");
            }
        }
    }
}
