using BlackStar.Functions;
using BlackStar.Model;
using BlackStar.Model.Interfaces;
using BlackStar.Rules.ListAttributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomActionCallingRule;

public partial class RuleLib踏歌
{
    /// <summary>
    /// 此处演示安排单个IAction动作，可根据需要改写成按一定顺序安排一批动作
    /// </summary>
    /// <param name="para动作名">IAction动作名字，对应CustomAction里面的类名（如Action运送，则“运送”为动作名）</param>
    /// <param name="paraSettings">初始设置</param>
    /// <param name="paraVariables">变量集合</param>
    /// <param name="para开始时间">开始时间DateTime的字符串形式</param>
    /// <param name="ruleID">自动生成的规则ID</param>
    [RuleComments("安排通过外部dll注入的IAction动作")]
    public void Rule安排注入动作(
        [IsDefaultParameter][ParaComments("IAction动作名字，对应CustomAction里面的类名（如Action运送，则“运送”为动作名）")] string para动作名,
        [ParaComments("初始设置")] string paraSettings,
        [ParaComments("变量集合")] string paraVariables,
        [ParaComments("开始时间DateTime的字符串形式")]string para开始时间,
        Guid ruleID)
    {
        DateTime dt = TimeOP.DateTimeFromString(para开始时间);

        var actionQuery = ActionLoader.Actions.Where(i => i.Key == para动作名);
        if (actionQuery.Count()==0)
        {
            CaseLogOP.WriteMessage($"指定的动作\"{para动作名}\"不存在");
            return;
        }

        //解析settings初始设置
        Dictionary<string, object> settings = new();
        foreach (var pair in StringOP.StringListFromStringByDevider(paraSettings, ","))
        {
            string[] keyvalue = StringOP.DivideInTwoByDivider(pair, "=");
            settings.Add(keyvalue[0], keyvalue[1]);
        }

        //解析variables变量集合
        Dictionary<string, object> variables = new();
        foreach (var pair in StringOP.StringListFromStringByDevider(paraVariables, ","))
        {
            string[] keyvalue = StringOP.DivideInTwoByDivider(pair, "=");
            variables.Add(keyvalue[0], keyvalue[1]);
        }

        BuildTempo(dt, dt+TimeSpan.FromHours(4));   //创建信号节拍

        //所有动作的时机，这里需要通过算法确定每个动作的顺序和起始时机，或者在迭代过程中逐个确定，不一定都如下
        //待优化
        List<DateTime> actionTimes = new();
        actionTimes.Add(dt);
        actionTimes.Add(dt);
        actionTimes.Add(dt + TimeSpan.FromMinutes(10));
        actionTimes.Add(dt + TimeSpan.FromMinutes(15));
        actionTimes.Add(dt + TimeSpan.FromMinutes(15));
        actionTimes.Add(dt + TimeSpan.FromMinutes(20));
        actionTimes.Add(dt + TimeSpan.FromMinutes(25));

        string actionid = "";
        int no = 0;
        foreach (DateTime actionDt in actionTimes)   //针对每一个动作起始时机
        {
            no++;
            IAction action = Activator.CreateInstance(actionQuery.First().Value) as IAction;     //创建动作实例

            //安排该动作，会将结果自动写入算例
            action.Arrange(this.CaseLogOP,
                this.SampleCase.dsSampleCase,
                settings,
                new Dictionary<string, object>(variables),  //由于上一个动作的变量已更改，复制原始变量集
                dt,
                no.ToString(),
                ruleID);
        }
    }

    /// <summary>
    /// 确定信号节拍——这里最重要，是优化算法的主要决策内容，信号周期、方向，写入this.SampleCase.dsSampleCase中
    /// </summary>
    private void BuildTempo(DateTime startDt, DateTime endDt)
    {

        #region 道路节拍

        //待优化：道路的上下行节拍、相位差，上行时长、下行时长
        List<(string, float, float, float)> roadTempos = new()
        {
            ("R14",-2,5,3),  //路，起始时间偏移，上行时长，下行时长
            ("R24",-2,6,4),
            ("R34",-2,5,2),
            ("R45",-2,7,4),
            ("R56",-2,6,4),
            ("R67",-2,5,3),
            ("R78",-2,3,2),
            ("R89",-2,6,3),
            ("R810",-2,7,4),
            ("R811",-2,5,4),
        };

        foreach(var roadTempo in roadTempos)
        {
            DateTime dt = startDt + TimeSpan.FromMinutes(roadTempo.Item2);
            while(dt<endDt)
            {
                //上行
                DataSetSampleCase.dtAvailabilityRow avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.资源代号 = roadTempo.Item1;
                avail.开始 = dt;
                avail.结束 = dt + TimeSpan.FromMinutes(roadTempo.Item3);
                avail.服务代号 = "PassUp";
                avail.可用性ID = Guid.NewGuid();
                avail.可用性描述 = "1";
                avail.可用性类型 = "常值";
                avail.启用 = true;
                this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);
                dt += TimeSpan.FromMinutes(roadTempo.Item3);

                //下行
                avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.资源代号 = roadTempo.Item1;
                avail.开始 = dt;
                avail.结束 = dt + TimeSpan.FromMinutes(roadTempo.Item4);
                avail.服务代号 = "PassDown";
                avail.可用性ID = Guid.NewGuid();
                avail.可用性描述 = "1";
                avail.可用性类型 = "常值";
                avail.启用 = true;
                this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);
                dt += TimeSpan.FromMinutes(roadTempo.Item4);
            }
        }
        #endregion

        #region 路口节拍
        //待优化：路口的上下行节拍、相位差，上行时长、下行时长
        List<(string Cross, float StartOffset, float UpDuration, float DownDuration)> crossTempos = new()
        {
            ("Cross4",-2,10,4),  //路口，起始时间偏移，上行时长，下行时长
            ("Cross8",-6,8,3),
        };

        foreach (var crossTempo in crossTempos)
        {
            DateTime dt = startDt + TimeSpan.FromMinutes(crossTempo.Item2);
            while (dt < endDt)
            {
                //上行
                DataSetSampleCase.dtAvailabilityRow avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.资源代号 = crossTempo.Item1;
                avail.开始 = dt;
                avail.结束 = dt + TimeSpan.FromMinutes(crossTempo.Item3);
                avail.服务代号 = "PassUp";
                avail.可用性描述 = "1";
                avail.可用性ID = Guid.NewGuid();
                avail.可用性类型 = "常值";
                avail.启用 = true;
                this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);
                dt += TimeSpan.FromMinutes(crossTempo.Item3);

                //下行
                avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.资源代号 = crossTempo.Item1;
                avail.开始 = dt;
                avail.结束 = dt + TimeSpan.FromMinutes(crossTempo.Item4);
                avail.服务代号 = "PassDown";
                avail.可用性描述 = "1";
                avail.可用性ID = Guid.NewGuid();
                avail.可用性类型 = "常值";
                avail.启用 = true;
                this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);
                dt += TimeSpan.FromMinutes(crossTempo.Item4);
            }
        }
        #endregion

        #region 会车区节拍
        List<string> storeTempos =  new()
        {
            "n5","n6","n7",
        };

        foreach (var store in storeTempos)
        {
            DataSetSampleCase.dtAvailabilityRow avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
            avail.资源代号 = store;
            avail.开始 = startDt;
            avail.结束 = endDt;
            avail.服务代号 = "Store";
            avail.可用性描述 = "1";
            avail.可用性ID = Guid.NewGuid();
            avail.可用性类型 = "常值";
            avail.启用 = true;
            this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
            this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
            this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);
        }
        #endregion

        #region 电铲站节拍
        //待优化，需要给出合理的矿石产生节拍
        List<(string, string, float, float, float)> loadStationTempos = new()
        {
            ( "s1", "LoadHigh", -1,5,3),    //工作站，服务，起始时间，服务时间，间隔
            ( "s2", "LoadMix", -1,6,3),
            ( "s3", "LoadLow", -1,7,3),
        };

        foreach (var station in loadStationTempos)
        {
            DateTime dt = startDt + TimeSpan.FromMinutes(station.Item3);
            while (dt < endDt)
            {
                DataSetSampleCase.dtAvailabilityRow avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
                avail.资源代号 = station.Item1;
                avail.开始 = dt;
                avail.结束 = dt + TimeSpan.FromMinutes(station.Item4);
                avail.服务代号 = station.Item2;
                avail.可用性描述 = "1";
                avail.可用性ID = Guid.NewGuid();
                avail.可用性类型 = "常值";
                avail.启用 = true;
                this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
                this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
                this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);
                dt+= TimeSpan.FromMinutes(station.Item4 + station.Item5);

                //另外需要将站的变量改写为适当的值，用以标记站的状态，请使用：
                //this.SampleCase.dsSampleCase.dtFact.GetVariable(...);
                //this.SampleCase.dsSampleCase.dtFact.SetVariable(...);
            }
        }
        #endregion

        #region 破碎站节拍
        //待优化，需要给出合理的矿石产生节拍
        List<(string Station, string Service, float Start, float Interval)> breakStationTempos = new()
        {
            //工作站，服务，起始时间，服务时间，间隔
            ( "b9", "UnloadHigh", -1,3),
            ( "b10", "UnloadMix", -1,2),
            ( "b11", "UnloadLow", -1,2),
        };

        foreach (var station in breakStationTempos)
        {
            DataSetSampleCase.dtAvailabilityRow avail = this.SampleCase.dsSampleCase.dtAvailability.NewdtAvailabilityRow();
            avail.资源代号 = station.Item1;
            avail.开始 = startDt;
            avail.结束 = endDt;
            avail.服务代号 = station.Item2;
            avail.可用性描述 = "1";
            avail.可用性ID = Guid.NewGuid();
            avail.可用性类型 = "常值";
            avail.启用 = true;
            this.SampleCase.dsSampleCase.dtAvailability.AdddtAvailabilityRow(avail);
            this.SampleCase.dsSampleCase.dtRemain.CreateInitialRemain(avail);
            this.SampleCase.dsSampleCase.dtResourceAvailability.CreateResource(avail);

            //另外需要将站的变量改写为适当的值，用以标记站的状态，请使用：
            //this.SampleCase.dsSampleCase.dtFact.GetVariable(...);
            //this.SampleCase.dsSampleCase.dtFact.SetVariable(...);
        }
        #endregion
    }
}
