using BlackStar.Model;
using BlackStar.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using MoreLinq.Experimental;
using BlackStar.Algorithms;
using System.Text.RegularExpressions;

/// <summary>
/// an Action library should be name as ActionLibXXXXX
/// </summary>
namespace ActionLibMyActionLib;

/// <summary>
/// an Action extention should be named as ActionXXXXX
/// </summary>
public class Action运送 : IAction
{
    /// <summary>
    /// 初始化动作设置：出发地点，等
    /// </summary>
    public Dictionary<string, object> Settings { get; set; }

    /// <summary>
    /// 动作状态：已装载，已卸载，地点，当前方向，等
    /// </summary>
    public Dictionary<string, object> Variables { get ; set ; }

    private DataSetSampleCase _dsSampleCase;

    public string ActionName { get ; set; }

    #region 路网
    //private Dictionary<string, string> _loadStations = new()
    //{
    //    { "电铲1", "高磁"},
    //    { "电铲2", "混合"},
    //    { "电铲3", "低磁"}
    //};

    //private Dictionary<string, string> _unloadStations = new()
    //{
    //    { "破碎9", "高磁"},
    //    { "破碎10", "混合"},
    //    { "破碎11", "低磁"}
    //};


    /// <summary>
    /// 每个站信息： id，站代号，站工作成本，是折返点，是完成卸载点，上行服务，下行服务，消耗容量
    /// </summary>
    private List<(int Id, string Station, int StationCost, bool IsReturning, bool IsFinish, string UpService, string DownService, float Consume)> _stations = new()
    {
        (1,"s1",5, true, false, "LoadHigh", "LoadHigh", 1),
        (2,"s2",6, true, false, "LoadMix", "LoadMix", 1),
        (3,"s3",7, true, false, "LoadLow", "LoadLow", 1),
        (4,"Cross4",3, false, false, "PassUp", "PassDown", 0.01f),
        (5,"n5",4, false, false, "Store", "Store", 0.01f),
        (6,"n6",5, false, false, "Store", "Store", 0.01f),
        (7,"n7",4, false, false, "Store", "Store", 0.01f),
        (8,"Cross8",3, false, false, "PassUp", "PassDown",0.01f),


        (9,"b9",3, true, true, "UnloadHigh", "UnloadHigh", 1),
        (10,"b10",2, true, true, "UnloadMix", "UnloadMix", 1),
        (11,"b11",3, true, true,"UnloadLow", "UnloadLow", 1),
    };

    /// <summary>
    /// 邻接表形式的弧，节点号1、节点号2、路代号、上行成本、下行成本
    /// </summary>
    private List<(string Node1, string Node2, string Edge, int UpCost, int DownCost)> _links = new()
    {
        ("s1","Cross4","R14",5,3),  //节点1，节点2代号，路代号，上行成本，下行成本
        ("s2","Cross4","R24",6,4),
        ("s3","Cross4","R34",5,2),
        ("Cross4","n5","R45",7,4),
        ("n5","n6","R56",6,4),
        ("n6","n7","R67",5,3),
        ("n7","Cross8","R78",3,2),
        ("Cross8","b9","R89",6,3),
        ("Cross8","b10","R810",7,4),
        ("Cross8","b11","R811",5,4),
    };
    #endregion

    /// <summary>
    /// 安排该动作，继承自IAction接口
    /// </summary>
    /// <param name="caseLogOP">当前算例logger实例</param>
    /// <param name="dsSampleCase">当前算例</param>
    /// <param name="settings">初始化参数集</param>
    /// <param name="variables">变量集</param>
    /// <param name="startDt">开始时间</param>
    void IAction.Arrange(SampleCaseLogOP caseLogOP, DataSetSampleCase dsSampleCase, Dictionary<string, object> settings, Dictionary<string, object> variables, DateTime startDt, string actionId, Guid guid)
    {
        _dsSampleCase = dsSampleCase;
        Settings = settings;
        Variables = variables;
        DateTime currentTime = startDt;
        string startLocation = (string)Settings["出发地点"];
        string currentLocation = startLocation;
        Variables.Add("地点",Settings["出发地点"]);
        Variables.Add("当前方向", "下行");
        Variables.Add("完成卸载", false);
        string currentDirection = Variables["当前方向"] as string;
        bool finished = false;
        bool lastStationIsReturnStation=false;
        //移动、装、卸
        while(!finished)
        {
            #region 判断折返
            if (lastStationIsReturnStation)
            {
                if (Variables["当前方向"] as string == "上行")
                {
                    Variables["当前方向"] = "下行";
                    currentDirection = "下行";
                }
                    
                else if (Variables["当前方向"] as string == "下行")
                {
                    Variables["当前方向"] = "上行";
                    currentDirection = "上行";
                }
                    
            }
            #endregion

            string nextLocation = SelectNextLocation();

            #region 弧上移动
            var edgeInfo = getEdge(currentLocation, nextLocation, currentDirection);
            string serviceCode = currentDirection == "上行" ? "PassUp" : "PassDown";
            List<DecomposeConsume> consumesEdge = new(); //对弧的服务消耗列表
            consumesEdge.Add(new()
            {
                使用源 = "运送" + actionId,
                服务代号 = serviceCode,
                资源代号 = edgeInfo.Item1,
                消耗常值 = 0.01f,
                //消耗常值 = 0f,
                消耗模型 = ConsumeTypeModels.常值,
                开始相对时间 = TimeSpan.Zero,
                结束相对时间 = edgeInfo.Item2
            });
            if(AlgorithmOP.TryConsumes(_dsSampleCase, consumesEdge, ref currentTime, PostponeOption.后移, TimeSpan.FromMinutes(30), false, edgeInfo.Item1, ""))
            {
                _dsSampleCase.dtRemain.PerformConsumes(consumesEdge, guid);
            }
            else
            {
                caseLogOP.WriteMessage("无法安排");
            }
            #endregion

            currentTime = currentTime + edgeInfo.Item2;

            #region 站上工作
            var stationInfo = getStationInfo(nextLocation, currentDirection);
            List<DecomposeConsume> consumesStation = new(); //对站的服务消耗列表
            consumesStation.Add(new()
            {
                使用源 = "运送" + actionId,
                服务代号 = currentDirection == "上行" ? stationInfo.Item6 : stationInfo.Item7,
                资源代号 = stationInfo.Item2,
                //消耗常值 = 0.01f,
                消耗常值 = stationInfo.Rest.Item1,
                消耗模型 = ConsumeTypeModels.常值,
                开始相对时间 = TimeSpan.Zero,
                结束相对时间 = TimeSpan.FromMinutes(stationInfo.Item3)
            });

            //待优化，如果要根据资源变量进行试拍，请使用TryConsumes的变量版本
            if (AlgorithmOP.TryConsumes(_dsSampleCase, consumesStation, ref currentTime, PostponeOption.后移, TimeSpan.FromMinutes(30), false, stationInfo.Item2, ""))
            {
                _dsSampleCase.dtRemain.PerformConsumes(consumesStation, guid);
            }
            else
            {
                caseLogOP.WriteMessage("无法安排");
            }

            currentTime = currentTime + TimeSpan.FromMinutes(stationInfo.Item3);

            lastStationIsReturnStation = stationInfo.Item4;

            if(stationInfo.Item5)
                Variables["完成卸载"] = true;

            //完成卸载并回到原点，结束一个完整动作
            if (Variables["地点"] as string == startLocation && (bool)Variables["完成卸载"])
            {
                finished = true;
            }
            else
            {
                currentLocation = nextLocation;
                Variables["地点"] = nextLocation;
            }
            #endregion

            //break;
        }

        //选出任意运载车辆并核算车辆能力
        DataSetBlackStar.dtResourceRow vehicle = SelectVehicle(startDt, TimeSpan.FromMinutes(30));
        if (vehicle == null)
        {
            caseLogOP.WriteMessage("没有可用矿卡");
            return;
        }

        List<DecomposeConsume> consumesTruck = new();
        //待优化，对矿卡的服务消耗列表
        //consumesTruck.Add(new DecomposeConsume()
        //{

        //});
        if (AlgorithmOP.TryConsumes(_dsSampleCase, consumesTruck, ref currentTime, PostponeOption.后移, TimeSpan.FromMinutes(30), false, "",""))
        {
            _dsSampleCase.dtRemain.PerformConsumes(consumesTruck, guid);
        }

        
    }

    private string serviceFromStation(string stationCode, string currentDirection)
    {
        if(stationCode== "n5" || stationCode == "n5" || stationCode == "n7")
        {
            return "Store";
        }
        if(stationCode == "Cross4" || stationCode == "Cross8")
        {
            if (currentDirection == "上行" )
            {
                return "PassUp";
            }
            else if (currentDirection == "下行" )
            {
                return "PassDown";
            }
        }
        return "";
    }

    private (string Edge, TimeSpan Cost) getEdge(string currentLocation, string nextLocation, string currentDirection)
    {
        string edgeCode="";
        TimeSpan edgeCost = TimeSpan.Zero;
        if (currentDirection=="上行")
        {
            var link = this._links.Where(
                i => i.Node1 == currentLocation && i.Node2 == nextLocation
                ).FirstOrDefault();
            if (link == ( "","","",0,0))
                edgeCode = "";
            else
            {
                edgeCode = link.Item3;
                edgeCost = TimeSpan.FromMinutes(link.Item4);
            }
        }
        else if(currentDirection == "下行")
        {
            var link = this._links.Where(
                 i => i.Item2 == currentLocation && i.Item1 == nextLocation
                ).FirstOrDefault();
            if (link == ("", "", "", 0, 0))
                edgeCode = "";
            else
            {
                edgeCode = link.Item3;
                edgeCost = TimeSpan.FromMinutes(link.Item5);
            }
        }
        return (edgeCode, edgeCost);
    }

    private (int, string, int, bool, bool, string, string, float) getStationInfo(string station, string currentDirection)
    {
        return this._stations.Where(i => i.Item2 == station).FirstOrDefault();
    }

    private int numberFromLocationName(string locationName)
    {
        const string pattern = @"(?<number>\d+)";
        Match m = Regex.Match(locationName, pattern);
        if(m.Success)
        {
            return int.Parse(m.Result("${number}"));
        }
        return 0;
    }

    private string SelectNextLocation()    //在列表里找下一个地点
    {
        string direction = Variables["当前方向"] as string;
        string current = Variables["地点"] as string;
        
        if(direction=="上行")
        {
            var nextLocations = _links.Where(i => i.Item1 == current);
            if(nextLocations.Count()==0)    //到头
            {
                var backLocations = _links.Where(i => i.Item2 == current);
                return backLocations.First().Item1;
            }
            else if(nextLocations.Count()==1)
            {
                return nextLocations.FirstOrDefault().Item2;
            }
            else
            {
                //待优化：先用随机选站，还应加上按装料种类（Variables["矿料种类"]）选站，读取兵改写相关变量，请使用：
                //_dsSampleCase.dtFact.GetVariable(...);
                //_dsSampleCase.dtFact.SetVariable(...);
                return nextLocations.RandomSubset(1).FirstOrDefault().Item2;
            }
        }
        else if(direction=="下行")
        {
            var nextLocations = _links.Where(i => i.Item2 == current);
            if (nextLocations.Count() == 0)
            {
                var backLocations = _links.Where(i => i.Item1 == current);
                return backLocations.First().Item2;
            }
            else if (nextLocations.Count() == 1)
            {
                return nextLocations.FirstOrDefault().Item1;
            }
            else
            {
                //待优化：先用随机选站，还应加上按装料种类（Variables["矿料种类"]）选站，读取兵改写相关变量，请使用：
                //_dsSampleCase.dtFact.GetVariable(...);
                //_dsSampleCase.dtFact.SetVariable(...);
                return nextLocations.RandomSubset(1).FirstOrDefault().Item1;
            }
        }
        return "";
    }

    /// <summary>
    /// 路口选站
    /// </summary>
    /// <param name="dt">the DateTime when to select next station</param>
    /// <returns></returns>
    //private string SelectWorkStation(DateTime dt)
    //{
    //    string ret = "";
    //    string direction = Variables["当前方向"] as string;
    //    bool hasLoaded = (bool)Variables["已装载"];
    //    string location = Variables["地点"] as string;
    //    if (direction == "下行" && !hasLoaded)        //去电铲站
    //    {
    //        ret =  this._loadStations.Where(i => i.Value == "低磁" || i.Value == "混合")
    //            .MinBy(i=> GetQueueLength(i.Key, dt))
    //           .Select(i => i.Key).FirstOrDefault();
    //    }
    //    else if (direction == "上行" && hasLoaded)    //去破碎站
    //    {
    //        ret = this._unloadStations.Where(i => i.Value == "低磁" || i.Value == "混合")
    //            .MinBy(i => GetQueueLength(i.Key, dt))
    //           .Select(i => i.Key).FirstOrDefault();
    //    }
    //    return ret;
    //}

    private void SetQueueLength(string resource, DateTime dt, int length, TimeSpan ts )
    {
        _dsSampleCase.dtFact.SetVariable(resource, "排队长度", length.ToString(), dt,ts );
    }

    private int GetQueueLength(string resource, DateTime dt)
    {
        var variable = _dsSampleCase.dtFact.GetVariable(resource, "排队长度", dt);
        string stringResult = variable.Value.Description;
        if (stringResult == "")
            return int.MaxValue;
        int intResult = 0;
        if (int.TryParse(stringResult, out intResult))
            return intResult;
        return int.MaxValue;
    }

    private DataSetBlackStar.dtResourceRow SelectVehicle(DateTime startDt, TimeSpan ts)
    {
        //待优化：选取车辆，先给出随机策略
        var query = EnvModel.dsBlackStar.dtResource.Rows.Cast<DataSetBlackStar.dtResourceRow>()
            .Where(i => i.资源类别 == "车辆").RandomSubset(1).FirstOrDefault();
        return query;
    }

}

