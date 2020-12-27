using System;
using BlackStar.Model;
using BlackStar.Algorithms;
using BlackStar.USL;
using BlackStar.Rules;
using BlackStar.EventAggregators;

namespace HelloBlackStar
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello BlackStar-APS!");

            //Console.ReadLine();
            EnvModel.InitializeEnvModel();
            Console.WriteLine(LicensingOP.LicenseInfo);
            USLManagerOP.InitializeUSLOP();
            USLManagerOP.dsUSL.ReadXml("default.usl");  //读入USL基本配置
            CreateModel();  //创建默认型号
            CreateResourcesServices();      //创建资源服务原型
            DataSetBlackStar.dtSampleCaseRow samplecase = CreateSampleCase();     //创建算例

            RuleOP.InitilizeRuleOP();   //初始化RuleOP

            CreateAction();         //创建动作需求
            CreateAvailability();   //创建可用性
            CreateDemand();           //创建规则
            EnvModel.dsBlackStar.WriteXml("dsBlackStar.bs");

            BlackStar.EventAggregators.EventOP.OverAllNotifyEventAggregator.GetEvent<NotifyEvent>().Subscribe(NotifyEventHandler, true);    //监听规则执行完毕事件
            RuleOP.Execute("算例");   //开始执行规则
            while (true)
                Console.ReadLine();

            //samplecase.dsSampleCase.WriteXml("dsSampleCase.bs");
        }

        private static void NotifyEventHandler(string NotifyMessage)
        {
            switch (NotifyMessage)
            {
                case "RuleExecuteComplete":
                    PrintResult();
                    break;
            }
        }

        private static void CreateDemand()
        {

            //规则
            Guid id = Guid.NewGuid();
            DataSetBlackStar.dtRuleRow rule = EnvModel.dsBlackStar.dtRule.NewdtRuleRow();
            rule.规则ID = id;
            rule.分组 = "默认";
            rule.规则 = "通用：用简单约束安排动作";
            rule.默认参数 = "安排表达式";
            rule.取值 = "2020-1-1 0:0:0 ：delivery";
            rule.有效 = true;
            //rule.组内顺序 = 1;
            EnvModel.dsBlackStar.dtRule.AdddtRuleRow(rule);

            rule.SetPara("顺延选项", "后移");
            rule.SetPara("最大顺延", "1d");
            rule.SetPara("允许更换资源", "False");
        }

        private static void CreateModel()
        {
            DataSetBlackStar.dtModelRow model = EnvModel.dsBlackStar.dtModel.NewdtModelRow();
            model.型号族 = "车队";
            model.启用 = true;
            EnvModel.dsBlackStar.dtModel.AdddtModelRow(model);

            DataSetBlackStar.dtModelConfigRow config = EnvModel.dsBlackStar.dtModelConfig.NewdtModelConfigRow();
            config.型号族 = "车队";
            config.参数名 = "特征点正则表达式";
            config.参数值 = @"T[a-z_]*\d*";
            EnvModel.dsBlackStar.dtModelConfig.AdddtModelConfigRow(config);

            EnvModel.CurrentModel = "车队";
        }

        private static void Solve(DataSetBlackStar.dtSampleCaseRow samplcase)
        {
            bool result = AlgorithmOP.ArrangeActionSimple(
                samplcase,
                "delivery",
                new DateTime(2020, 1, 1, 0, 0, 0),
                 PostponeOption.后移,
                 TimeSpan.FromDays(1),
                 false,
                 Guid.Empty,
                 true,
                 "",
                 ""
                );
            Console.WriteLine("安排成功：" + result.ToString());
            Console.WriteLine("事件：");
        }

        private static void PrintResult()
        {
            DataSetBlackStar.dtSampleCaseRow samplcase = EnvModel.dsBlackStar.dtSampleCase.Rows[0] as DataSetBlackStar.dtSampleCaseRow;
            Console.WriteLine("事件结果：");
            foreach (DataSetSampleCase.dtEventRow eventrow in samplcase.dsSampleCase.dtEvent.Rows)
            {
                Console.WriteLine($"{eventrow.事件名称} {eventrow.事件代号} {eventrow.开始} {eventrow.结束}");
            }
        }

        private static void CreateAction()
        {
            //创建一个动作类别
            DataSetBlackStar.dtActionCategoryRow category = EnvModel.dsBlackStar.dtActionCategory.NewdtActionCategoryRow();
            category.终结动作 = true;
            category.动作类别 = "默认动作类别";
            category.动作代号模式 = @"\S+";
            EnvModel.dsBlackStar.dtActionCategory.AdddtActionCategoryRow(category);

            DataSetBlackStar.dtActionCategoryUnityRow unity = EnvModel.dsBlackStar.dtActionCategoryUnity.NewdtActionCategoryUnityRow();
            //unity.公共元素表达式 = "(t ：consume large1 carry 20sec) 或 (t ：consume small1 carry 20sec) ";
            unity.公共元素表达式 = "t ：consume carry 20sec";
            unity.动作类别 = "默认动作类别";
            EnvModel.dsBlackStar.dtActionCategoryUnity.AdddtActionCategoryUnityRow(unity);

            //创建一个动作
            DataSetBlackStar.dtActionRow action = EnvModel.dsBlackStar.dtAction.NewdtActionRow();
            action.动作类别 = "默认动作类别";
            action.动作名称 = "运送";
            action.动作代号 = "delivery";
            EnvModel.dsBlackStar.dtAction.AdddtActionRow(action);
            //创建动作的具体步骤
            DataSetBlackStar.dtActionElementRow element = EnvModel.dsBlackStar.dtActionElement.NewdtActionElementRow();
            element.动作代号 = "delivery";
            element.元素代号 = "delivery";
            element.元素表达式 = "";
            element.顺序 = 1;
            element.约束 = "t";
            element.持续 = "20s";
            EnvModel.dsBlackStar.dtActionElement.AdddtActionElementRow(element);
        }

        private static DataSetBlackStar.dtSampleCaseRow CreateSampleCase()  //创建算例
        {
            DataSetBlackStar.dtSampleCaseRow sample = EnvModel.dsBlackStar.dtSampleCase.NewdtSampleCaseRow();
            sample.主算例 = true;
            sample.算例 = "算例1";
            sample.InitilizeSampleCase();
            EnvModel.dsBlackStar.dtSampleCase.AdddtSampleCaseRow(sample);
            return sample;
        }

        private static void CreateResourcesServices()
        {

            //创建车辆资源类别
            DataSetBlackStar.dtResourceCategoryRow category = EnvModel.dsBlackStar.dtResourceCategory.NewdtResourceCategoryRow();
            category.资源类别 = "车辆";
            EnvModel.dsBlackStar.dtResourceCategory.AdddtResourceCategoryRow(category);

            //创建车辆资源
            DataSetBlackStar.dtResourceRow resource1 = EnvModel.dsBlackStar.dtResource.NewdtResourceRow();
            resource1.资源名称 = "大型车1";
            resource1.资源代号 = "large1";
            resource1.资源类别 = "车辆";
            DataSetBlackStar.dtResourceRow resource2 = EnvModel.dsBlackStar.dtResource.NewdtResourceRow();
            resource2.资源名称 = "小型车1";
            resource2.资源代号 = "small1";
            resource2.资源类别 = "车辆";
            EnvModel.dsBlackStar.dtResource.AdddtResourceRow(resource1);
            EnvModel.dsBlackStar.dtResource.AdddtResourceRow(resource2);

            //创建服务种类
            DataSetBlackStar.dtServiceCategoryRow servcieCategory = EnvModel.dsBlackStar.dtServiceCategory.NewdtServiceCategoryRow();
            servcieCategory.服务类别 = "默认服务类别";
            EnvModel.dsBlackStar.dtServiceCategory.AdddtServiceCategoryRow(servcieCategory);

            //创建服务
            DataSetBlackStar.dtServiceRow service = EnvModel.dsBlackStar.dtService.NewdtServiceRow();
            service.服务类别 = "默认服务类别";
            service.服务代号 = "carry";
            service.服务名称 = "运载";
            EnvModel.dsBlackStar.dtService.AdddtServiceRow(service);

            foreach (DataSetBlackStar.dtRuleRow rule in EnvModel.dsBlackStar.dtRule.Rows)
            {
                Console.WriteLine($"{rule.组内顺序}   {rule.取值}");
            }
        }

        private static void CreateAvailability()    //创建可用性
        {
            //规则分组
            DataSetBlackStar.dtRuleGroupRow group = EnvModel.dsBlackStar.dtRuleGroup.NewdtRuleGroupRow();
            group.分组 = "默认";
            group.分组顺序 = 1;
            EnvModel.dsBlackStar.dtRuleGroup.AdddtRuleGroupRow(group);

            //创建算例可用性

            for (int i = 0; i < 8; i++)
            {
                DataSetBlackStar.dtRuleRow rule = EnvModel.dsBlackStar.dtRule.NewdtRuleRow();
                rule.分组 = "默认";
                rule.规则ID = Guid.NewGuid();
                rule.规则 = "通用：创建资源服务";
                rule.默认参数 = "可用性表达式";
                rule.取值 = $"{new DateTime(2020, 1, 1, 8 + i, 0, 0)} ~ {new DateTime(2020, 1, 1, 8 + i, 10, 0)} ：large1 carry 1";
                EnvModel.dsBlackStar.dtRule.AdddtRuleRow(rule);
            }

            for (int i = 0; i < 16; i++)
            {
                DataSetBlackStar.dtRuleRow rule = EnvModel.dsBlackStar.dtRule.NewdtRuleRow();
                rule.分组 = "默认";
                rule.规则ID = Guid.NewGuid();
                rule.规则 = "通用：创建资源服务";
                rule.默认参数 = "可用性表达式";
                rule.取值 = $"{new DateTime(2020, 1, 1, 8 + i, 0, 0)} ~ {new DateTime(2020, 1, 1, 8 + i, 10, 0)} ：small1 carry 1";
                EnvModel.dsBlackStar.dtRule.AdddtRuleRow(rule);
            }
        }
    }
}
