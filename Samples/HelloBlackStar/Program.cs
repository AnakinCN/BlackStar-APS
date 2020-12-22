using System;
using System.Linq;
using BlackStar;
using BlackStar.Model;
using BlackStar.Algorithms;
using BlackStar.USL;
using System.Xml.Schema;

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
            USLManagerOP.dsUSL.ReadXml("default.usl");

            CreateModel();  //创建默认型号
            CreateResourcesServices();      //创建资源服务原型
            DataSetBlackStar.dtSampleCaseRow samplcase = CreateSampleCase();     //创建算例
            CreateAvailability();   //创建可用性
            CreateAction();         //创建动作需求
            Solve(samplcase);       //求解安排动作
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
            DataSetSampleCase dsSample = new DataSetSampleCase(sample);
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
        }

        private static void CreateAvailability()    //创建可用性
        {
            //创建算例可用性
            DataSetBlackStar.dtSampleCaseRow sample = EnvModel.dsBlackStar.dtSampleCase.Rows.Cast<DataSetBlackStar.dtSampleCaseRow>().FirstOrDefault();

            for (int i = 0; i < 8; i++)
            {
                DataSetSampleCase.dtAvailabilityRow avail = sample.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.可用性ID = Guid.NewGuid();
                avail.资源代号 = "large1";
                avail.服务代号 = "carry";
                avail.可用性描述 = "large1 carry 1";
                avail.可用性类型 = "常值";
                avail.启用 = true;
                avail.开始 = new DateTime(2020, 1, 1, 8 + i, 0, 0); //每隔1小时
                avail.结束 = new DateTime(2020, 1, 1, 8 + i, 10, 0); //服务10分钟
                sample.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                sample.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                sample.dsSampleCase.dtResourceAvailability.CreateResource(avail);
            }

            for (int i = 0; i < 16; i++)
            {
                DataSetSampleCase.dtAvailabilityRow avail = sample.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.可用性ID = Guid.NewGuid();
                avail.资源代号 = "small1";
                avail.服务代号 = "carry";
                avail.可用性描述 = "small1 carry 1";
                avail.可用性类型 = "常值";
                avail.启用 = true;
                avail.开始 = new DateTime(2020, 1, 1, 8 + i, 0, 0); //每隔1小时
                avail.结束 = new DateTime(2020, 1, 1, 8 + i, 10, 0); //服务10分钟
                sample.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                sample.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                sample.dsSampleCase.dtResourceAvailability.CreateResource(avail);
            }

            foreach (DataSetSampleCase.dtAvailabilityRow avail in sample.dsSampleCase.dtAvailability.Rows)
            {
                Console.WriteLine($"{avail.资源代号} {avail.服务代号} {avail.开始.ToString("yyyy-MM-dd HH:mm")} {avail.结束.ToString("yyyy-MM-dd HH:mm")}");
            }
        }
    }
}
