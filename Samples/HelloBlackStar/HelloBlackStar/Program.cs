using System;
using System.Linq;
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
            CreateResources();      //创建资源
            CreateSampleCase();     //创建算例
            CreateAvailability();   //创建可用性
            CreateAction();         //创建动作需求
        }

        private static void CreateAction()
        {
            //创建一个动作类别
            DataSetBlackStar.dtActionCategoryRow category = EnvModel.dsBlackStar.dtActionCategory.NewdtActionCategoryRow();
            category.终结动作 = true;
            category.动作类别 = "默认动作类别";
            EnvModel.dsBlackStar.dtActionCategory.AdddtActionCategoryRow(category);
            //创建一个动作
            DataSetBlackStar.dtActionRow action = EnvModel.dsBlackStar.dtAction.NewdtActionRow();
            action.动作类别= "默认动作类别";
            action.动作名称 = "运送";
            action.动作代号 = "dilivery";
            EnvModel.dsBlackStar.dtAction.AdddtActionRow(action);
            //创建动作的具体步骤
            DataSetBlackStar.dtActionElementRow element = EnvModel.dsBlackStar.dtActionElement.NewdtActionElementRow();
            element.动作代号= "dilivery";
            element.顺序 = 1;
            element.约束 = "t";
            EnvModel.dsBlackStar.dtActionElement.AdddtActionElementRow(element);
        }

        private static void CreateSampleCase()  //创建算例
        {
            DataSetBlackStar.dtSampleCaseRow sample = EnvModel.dsBlackStar.dtSampleCase.NewdtSampleCaseRow();
            sample.主算例 = true;
            sample.算例 = "算例1";
            sample.InitilizeSampleCase();
            EnvModel.dsBlackStar.dtSampleCase.AdddtSampleCaseRow(sample);

            DataSetSampleCase dsSample = new DataSetSampleCase(sample);
        }

        private static void CreateResources()
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
            

        }

        private static void CreateAvailability()    //创建可用性
        {
            //创建可用性
            DataSetBlackStar.dtSampleCaseRow sample = EnvModel.dsBlackStar.dtSampleCase.Rows.Cast<DataSetBlackStar.dtSampleCaseRow>().FirstOrDefault();

            for(int i=0;i<8;i++)
            {
                DataSetSampleCase.dtAvailabilityRow avail = sample.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.可用性ID = Guid.NewGuid();
                avail.资源代号 = "large1";
                avail.服务代号 = "carry";
                avail.开始 = new DateTime(2020, 1, 1, 8 + i, 0, 0); //每隔1小时
                avail.结束 = new DateTime(2020, 1, 1, 8 + i, 10, 0); //服务10分钟
                sample.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
            }

            for (int i = 0; i < 16; i++)
            {
                DataSetSampleCase.dtAvailabilityRow avail = sample.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.可用性ID = Guid.NewGuid();
                avail.资源代号 = "small1";
                avail.服务代号 = "carry";
                avail.开始 = new DateTime(2020, 1, 1, 8 + i, 0, 0); //每隔1小时
                avail.结束 = new DateTime(2020, 1, 1, 8 + i, 10, 0); //服务10分钟
                sample.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
            }
        }
    }
}
