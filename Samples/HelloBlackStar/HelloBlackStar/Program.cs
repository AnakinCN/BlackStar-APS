using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackStar.Functions;
using BlackStar.Model;
using BlackStar.USL;

namespace BlackStar.Algorithms
{
    public static class AlgorithmOP     //安排动作算法
    {
        public enum PostponeOption
        {
            前移,后移,不顺移
        }

        public static bool ArrangeAction(DataSetBlackStar.dtSampleCaseRow dtSampleCaseRow,
            string actionCode, DateTime dtBase, PostponeOption postponeOption,
            TimeSpan maxPostpone, bool allowSwitch, Guid ruleID,
            bool toArrangeEvent=true, string desireResourceService= ""  , string desireTarget=""    //指定的资源、目标
            )        //安排动作树的根方法
        {
            if (ActionOP.IsFlexibleAction(actionCode))
                return ArrangeActionFlexible(dtSampleCaseRow, actionCode, dtBase, postponeOption, maxPostpone, ruleID);
            else
                return ArrangeActionSimple(dtSampleCaseRow, actionCode, dtBase, postponeOption, maxPostpone, allowSwitch, ruleID, toArrangeEvent, desireResourceService, desireTarget);
        }

        public static bool ArrangeActionSimple(DataSetBlackStar.dtSampleCaseRow dtSampleCaseRow,
            string actionCode, DateTime dtBase, PostponeOption postponeOption, TimeSpan maxPostpone,
            bool allowSwitch, Guid ruleID, bool toArrangeEvent, string desireResourceService, string desireTarget)
            //安排简单动作，支持复杂消耗：（t ：consume SZ11 RcRelay 3sec） 或 （t ：consume SZ11 RcGnd 3sec）
        {
            string resourceCode = EnvModel.dsBlackStar.dtResource.ResourceCode(desireResourceService);
            string targetCode = EnvModel.dsBlackStar.dtResource.ResourceCode(desireTarget);

            List<DecomposeEvent> deEvents = new List<DecomposeEvent>();
            List<DecomposeConsume> deConsumes = new List<DecomposeConsume>();
            try
            {
                ActionOP.DecomposeAction(actionCode, deEvents, deConsumes, TimeSpan.Zero, resourceCode);
//#if DEBUG
//                dtSampleCaseRow.CaseLogOP.WriteMessage("所需消耗：");
//                foreach(DecomposeConsume deconsume in deConsumes)
//                {
//                    dtSampleCaseRow.CaseLogOP.WriteMessage($"{deconsume.使用源} {deconsume.开始相对时间} {deconsume.服务代号} {deconsume.消耗常值}");
//                }
//                dtSampleCaseRow.CaseLogOP.WriteMessage($"消耗共{deConsumes.Count.ToString()}条");
//#endif
            }
            catch(Exception err)
            {
                dtSampleCaseRow.CaseLogOP.WriteMessage("未能分解动作: " + actionCode);
                dtSampleCaseRow.CaseLogOP.WriteMessageError(err.Message);
                dtSampleCaseRow.CaseLogOP.WriteMessageError(err.StackTrace);
                return false;
            }
            if (TryConsumes(dtSampleCaseRow.dsSampleCase, deConsumes, ref dtBase, postponeOption, maxPostpone, allowSwitch, resourceCode, targetCode))
            {
                //落实消耗至数据集
                dtSampleCaseRow.dsSampleCase.dtRemain.PerformConsumes(deConsumes, ruleID);    //待优化，可将PerformConsume与TryConsumes合并?

                //创建事件
                if (toArrangeEvent)
                    EventOP.CreateEvent(dtSampleCaseRow.dsSampleCase, deEvents, dtBase, ruleID);
                return true;
            }
            else
            {
                //if(dtSampleCaseRow.CaseLogOP != null)
                dtSampleCaseRow.CaseLogOP.WriteMessageGantt("未能安排动作: " + dtBase.ToString() + " " + actionCode + "，可用服务不足", dtBase);
                return false;
            }
        }

        public static bool ArrangeActionFlexible(DataSetBlackStar.dtSampleCaseRow dtSampleCaseRow,
            string ActionElementCode, DateTime dtBase,
            PostponeOption postponeOption, TimeSpan maxPostpone, Guid ruleID)            //安排动作树的根方法，柔性元素约束：tls+0 ~ tls+1800
        {
            DataSetSampleCase dsTry = null ;     
            dtSampleCaseRow.CaseLogOP.WriteMessage(ActionElementCode + "按柔性动作试排");

            DateTime rootDT = dtBase;           //整个动作基点
            DateTime actionDT = dtBase;         //当前搜索时间
            TimeSpan searchTS = TimeSpan.Zero;  //当前搜索时间同searchDT的偏移量;
            
            IEnumerable<DataSetBlackStar.dtActionElementRow> qElement = null;

            //StringBuilder sb = new StringBuilder();

            bool ActionOK = false;  //动作安排成功标志，迭代继续标准

            if (DataSetBlackStar.dtActionDataTable.IsPreDefinedAction(ActionElementCode, out qElement))
            {
                while(!ActionOK && Func.TimeOP.TimesSpanABS(rootDT - dtBase) <= maxPostpone)
                {
                    //sb.AppendLine();
                    //sb.AppendLine("rootDT=" + rootDT.ToString());
                    dsTry = dtSampleCaseRow.dsSampleCase.Copy();    //复制当前算例作为试排数据集

                    DateTime lastElementDT = rootDT;  //记录前一个元素开始时间，供tls约束使用
                    foreach (DataSetBlackStar.dtActionElementRow actionElementRow in qElement)  //遍历动作成员要素
                    {
                        string elementBase = "";
                        string elementCodeName = actionElementRow.元素代号 + " - " + actionElementRow.元素名称;
                        TimeSpan elementRangeStartTS = TimeSpan.Zero;      //元素时间同searchTS的偏移量
                        TimeSpan elementRangeEndTS = TimeSpan.Zero;
                        TimeSpan elementTS = TimeSpan.Zero;
                        TimeSpan elementMaxPostpone = TimeSpan.Zero;
                        PostponeOption elementPostponeOption = postponeOption;
                        if (Func.TimeOP.RangeTSFromConstrain(actionElementRow.约束, ref elementBase, ref elementRangeStartTS, ref elementRangeEndTS)) //元素允许时间范围
                        {
                            if (elementBase == "tls")   //从上一个元素开始点开始  tls+0 ~ tls+1800
                            {
                                actionDT = lastElementDT + elementRangeStartTS;
                                elementMaxPostpone = elementRangeEndTS - elementRangeStartTS;   //范围
                                elementPostponeOption = postponeOption;
                                //elementTS = lastElementTS + elementRangeStart;
                            }
                        }
                        else if (Func.TimeOP.BaseTSFromConstrain(actionElementRow.约束, ref elementBase, ref elementRangeStartTS))     //元素是相对于t的偏移 
                        {
                            if (elementBase == "t")     //从动作安排点开始  t-300
                            {
                                actionDT = rootDT + elementRangeStartTS;
                                elementMaxPostpone = TimeSpan.Zero;                     //固定时间点
                                elementPostponeOption = PostponeOption.不顺移;
                                //elementTS = searchTS + elementRangeStart;
                            }
                        }

                        List<DecomposeEvent> deElementEvents = new List<DecomposeEvent>();
                        List<DecomposeConsume> deElementConsumes = new List<DecomposeConsume>();
                        USLNode deConsumesRootNode = null;

                        IEnumerable<DataSetBlackStar.dtActionElementRow> qCurrentElement = null;
                        if (DataSetBlackStar.dtActionDataTable.IsPreDefinedAction(actionElementRow.元素代号, out qCurrentElement))   //元素他处展开定义
                        {

                        }
                        else
                        {
                            string elementCategory = EnvModel.dsBlackStar.dtActionCategory.EventTypeByCodePattern(actionElementRow.元素代号, dtSampleCaseRow.CaseLogOP);
                            if (elementCategory == "")      //本处原生定义，特征点：TXJ01A
                            {
                                ActionOP.ProcessElementChar(actionElementRow, deElementEvents, elementTS);
                            }
                            else                            //元素代号的模式符合某动作类别：TL010-HP377D，但是没有具体展开
                            {
                                UnityOP.ProcessUnityByCategory(ActionElementCode, deElementEvents, ref deConsumesRootNode, elementTS, elementCategory, " - " + actionElementRow.元素代号, "", true);
                                ActionOP.ProcessElementConsume(actionElementRow, ref deConsumesRootNode, elementTS, " - " + actionElementRow.元素代号);  //当处（元素表达式）定义的额外消耗 need TG02 RcGnd 25sec
                                ActionOP.ProcessElementChar(actionElementRow, deElementEvents, elementTS);
                            }
                        }

                        //sb.AppendLine(elementCodeName +
                        //   //" : dtBase=" + dtBase.ToString() +
                        //   //@" searchTS="+ searchTS.ToString() +
                        //   //" elementTS="+ elementTS.ToString() +
                        //   "  actionDT=" + actionDT.ToString() +
                        //   "  elementMaxPostpone=" + elementMaxPostpone.ToString()
                        //   );

                        if (deConsumesRootNode == null
                            && TryConsumes(dsTry, deElementConsumes, ref actionDT, elementPostponeOption, elementMaxPostpone, false, "", ""))
                            //简单消耗
                        {
                            //落实消耗至数据集
                            dsTry.dtRemain.PerformConsumes(deElementConsumes, ruleID);

                            //创建事件
                            //if (toArrangeEvent)
                            EventOP.CreateEvent(dsTry, deElementEvents, actionDT, ruleID);
                            ActionOK = true;
                            lastElementDT = actionDT;  //记录元素时间，供tls使用
                        }
                        else if (deConsumesRootNode != null &&
                                 TryConsumes(dsTry, deConsumesRootNode, deElementConsumes, ref actionDT, elementPostponeOption, elementMaxPostpone, false, "", ""))
                            //布尔树形式消耗
                        {
                            dsTry.dtRemain.PerformConsumes(deElementConsumes, ruleID);
                            //创建事件
                            //if (toArrangeEvent)
                            EventOP.CreateEvent(dsTry, deElementEvents, actionDT, ruleID);
                            ActionOK = true;
                            lastElementDT = actionDT;  //记录元素时间，供tls使用
                        }
                        else
                        {
                            //if(dtSampleCaseRow.CaseLogOP != null)
                            //dtSampleCaseRow.CaseLogOP.WriteMessageGantt("未能安排元素: " + dtBase.ToString() + " " + actionElementRow.元素代号 + "，可用服务不足", dtBase);
                            //dtSampleCaseRow.CaseLogOP.WriteMessage(sb.ToString());
                            switch(postponeOption)
                            {
                                case PostponeOption.前移:
                                    rootDT -= TimeSpan.FromSeconds(10);        //搜索步进
                                    break;
                                case PostponeOption.后移:
                                    rootDT += TimeSpan.FromSeconds(10);        //搜索步进
                                    break;
                                default:
                                    break;
                            }
                           
                            ActionOK = false;
                            break;
                        }
                    }
                }
            }
            if(ActionOK)
            {
                //dtSampleCaseRow.dsSampleCase.CopyFromWithoutLog(dsTry);
                //dtSampleCaseRow.dsSampleCase = dsTry.Copy();
                dtSampleCaseRow.dsSampleCase = dsTry;
                //dtSampleCaseRow.CaseLogOP.WriteMessage(sb.ToString());
                //Debug.WriteLine(sb.ToString());
                return true;
            }
            else
            {
                dtSampleCaseRow.CaseLogOP.WriteMessageGantt("未能安排元素: " + dtBase.ToString() + " " + ActionElementCode + "，可用服务不足", dtBase);
                return false;
            }
        }

        public static bool TryConsumes(DataSetSampleCase dsSampleCase,
            List<DecomposeConsume> deConsumes,
            ref DateTime dtBase, PostponeOption postponeOption,
            TimeSpan maxPostpone,  bool allowSwitch, string resource, string desireTargetCode) //判断在给定dtBase下是否能够同时满足所有Consumes
        {
            //待优化，仅剪裁相交的Availablility和Remain（如按绝对时间范围）

            TimeSpan totalPostpone = TimeSpan.Zero;

            //List<string> ServiceTypes = DecomposeConsume.ServiceTypesFromDeconsumes(deConsumes);      //仅指定服务
            List<Tuple<string,string,string>> ServiceResources = DecomposeConsume.ServiceResourceTargetsFromDeconsumes(deConsumes);        //服务-目标同时指定

            DateTime originBase = dtBase;
            DateTime searchBase = dtBase;
            TimeSpan currentPostPone = TimeSpan.Zero; //已经产生的推迟，总是大于等于0
            while (currentPostPone <= maxPostpone)
            {
                //dtSampleCaseRow.CaseLogOP.WriteMessage("------------试排时间：" + searchBase + "------------");

                TimeSpan allowedPostPone = maxPostpone - currentPostPone;
                TimeSpan maxServicePostPone = TimeSpan.Zero;  //最远延迟的量为多少，才能满足所有服务
                bool allServiceOK = true;
                //foreach (string serviceCode in ServiceTypes)==
                foreach (Tuple<string, string,string> tuple in ServiceResources)
                {
                    string serviceCode = tuple.Item1;
                    string resourceCode = tuple.Item2;
                    string targetCode = tuple.Item3;
                    TimeSpan servicePostPone = TimeSpan.Zero;

                    bool serviceOK = SearchServiceBase(deConsumes, postponeOption, allowedPostPone, searchBase,
                        ref servicePostPone, dsSampleCase, allowSwitch, serviceCode, resourceCode, targetCode);

                    if (!serviceOK) //每种服务、目标均需满足（暂时满足“且”关系，将来通过语法树待实现“或”）
                    {
                        if (servicePostPone == TimeSpan.Zero)
                            return false; // return false;   //找不到下一个可用位置

                        switch (postponeOption)
                        {
                            case PostponeOption.前移:
                                searchBase -= maxServicePostPone;
                                break;
                            case PostponeOption.后移:
                                searchBase += maxServicePostPone;
                                break;
                        }
                        currentPostPone = Func.TimeOP.TimesSpanABS(searchBase - originBase);
                        allServiceOK = false;
                        break; // 一种服务不满足，剩下的不继续测试
                    }
                    else
                    {
                        if (servicePostPone > maxServicePostPone)
                            maxServicePostPone = servicePostPone;
                    }
                }

                if (allServiceOK)
                {
                    switch (postponeOption)
                    {
                        case PostponeOption.前移:
                            searchBase -= maxServicePostPone;
                            break;
                        case PostponeOption.后移:
                            searchBase += maxServicePostPone;
                            break;
                    }
                    //searchBase += maxServicePostPone;
                    //全部Service都OK
                    foreach (DecomposeConsume deconsume in deConsumes)
                    {
                        deconsume.绝对基础 = searchBase; //记录试排结果
                        //deconsume.来源追踪 = ruleID + " " + deconsume.使用源 + " " + deconsume.资源代号 + " " + deconsume.服务代号;
                    }
                    dtBase = searchBase;
                    //dtSampleCaseRow.CaseLogOP.WriteMessage("------------试排时间：" + searchBase + "------------" + "True");
                    return true;
                }
                else
                {
                    if (maxServicePostPone == TimeSpan.Zero)
                        return false;
                }
            }
            //dtSampleCaseRow.CaseLogOP.WriteMessage("------------试排时间：" + searchBase + "------------" + "False");
            return false;   //超出最大延迟限制
        }

        public static bool TryConsumes(DataSetSampleCase dsSampleCase,
            USLNode tryNode, //根据布尔树关系试排
            List<DecomposeConsume> finalDeconsumes,
            ref DateTime dtBase, PostponeOption postponeOption,
            TimeSpan maxPostpone, bool allowSwitch, string resource, string desireTargetCode) //判断在给定dtBase下是否能够同时满足所有Consumes
        {
            bool ret = true;

            if (tryNode.Children.Count == 0 && tryNode.SymbolCode == "deconsume")
            {
                bool childOK = TryConsumes(dsSampleCase, tryNode.Content as List<DecomposeConsume>, ref dtBase, postponeOption, maxPostpone, allowSwitch,  resource, desireTargetCode);
                if (childOK)
                    finalDeconsumes.AddRange(tryNode.Content as List<DecomposeConsume>);
                return childOK;
            }
            else if (tryNode.Children.Count >= 1 && tryNode.SymbolCode == "AND")    //同时在一个时间点满足
            {
                DateTime dtSearch = dtBase;
                DateTime dtLast = dtBase;
                for (int i = 0; i < tryNode.Children.Count; )
                {
                    if (TryConsumes(dsSampleCase, tryNode[i], finalDeconsumes, ref dtSearch, postponeOption, maxPostpone, allowSwitch, resource, desireTargetCode))
                    {
                        if (dtSearch == dtLast)     
                        {
                            i++;
                        }
                        else               //两个and不在一个时间上满足
                        {
                            //RemovePreAddedConsumes(tryNode, tryNode[i], finalDeconsumes);
                            dtLast = dtSearch;
                            i = 0;         //重新测试
                            continue;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                dtBase = dtSearch;
                return true;
            }
            else if (tryNode.Children.Count >= 1 && tryNode.SymbolCode == "OR")
            {
                foreach (USLNode childnode in tryNode.Children)
                {
                    if (TryConsumes(dsSampleCase, childnode, finalDeconsumes, ref dtBase, postponeOption, maxPostpone, allowSwitch, resource, desireTargetCode))
                    {
                        return true;
                    }
                }
                return false;
            }
            return ret;
        }

        private static void RemovePreAddedConsumes(USLNode tryNode, USLNode childnode, List<DecomposeConsume> finalDeconsumes)   //滚回已经添加的consume
        {
            List<DecomposeConsume> todel = new List<DecomposeConsume>();
            IEnumerable<DecomposeConsume> qdeconsume = from DecomposeConsume consume in finalDeconsumes
                                                       where IsInPrevious(consume, tryNode, childnode)
                                                       select consume;
            foreach(DecomposeConsume consume in qdeconsume)
            {
                todel.Add(consume);
            }
            foreach (DecomposeConsume consume in todel)
            {
                finalDeconsumes.Remove(consume);
            }
        }

        private static bool IsInPrevious( DecomposeConsume consume, USLNode tryNode, USLNode childnode)
        {
            List<USLNode> prevs = new List<USLNode>();
            for(int i =0; i < tryNode.Children.Count; i++)
            {
                prevs.Add(tryNode[i]);
                if (tryNode[i] == childnode)
                    break;
            }
            foreach(USLNode node in prevs)
            {
                if (ContentContains(node, consume))
                    return true;
            }
            return false;
        }

        private static bool ContentContains(USLNode node, DecomposeConsume consume)
        {
            if (node.Content is DecomposeConsume)
            {
                if ((node.Content as DecomposeConsume) == consume)
                    return true;
            }
            else
            {
                foreach ( USLNode child in node.Children)
                {
                    if (ContentContains(child, consume))
                        return true;
                }
            }
            return false;
        }

        private static bool SearchServiceBase(List<DecomposeConsume> deConsumes, PostponeOption postponeOption, TimeSpan allowedPostPone, 
            DateTime searchBase, ref TimeSpan minServicePostPone, DataSetSampleCase dsSampleCase,
            bool allowSwitch, string serviceCode, string resourceCode, string targetCode)
        {
            //if (serviceCode == "YK")
            //    dtSampleCaseRow.CaseLogOP.WriteMessage("--------试排服务：" + serviceCode + "--------");

            DateTime serviceBase = searchBase; //满足当前服务的下一个位置
            bool serviceOK = false;

            List<DecomposeConsume> serviceDeConsumes = deConsumes.Where(i => i.服务代号 == serviceCode && i.资源代号 == resourceCode).ToList();

            if (allowSwitch)        //允许切换资源
            {
                serviceOK = SearchResourceBase(serviceDeConsumes, resourceCode, targetCode, ref serviceBase, postponeOption, allowedPostPone, dsSampleCase);
                TimeSpan servicePostpone = Func.TimeOP.TimesSpanABS(serviceBase - searchBase);
                if (serviceOK)
                {
                    if ((servicePostpone > TimeSpan.Zero && minServicePostPone > TimeSpan.Zero &&
                     servicePostpone < minServicePostPone)
                    || servicePostpone > TimeSpan.Zero && minServicePostPone == TimeSpan.Zero)
                        minServicePostPone = servicePostpone;
                }
            }
            else //不允许切换资源，多一层循环，确定该当前service应由哪个资源来提供
            {
                #region 不允许切换资源

                List<string> resources = new List<string>() { resourceCode };
                    //提供当前service的可选资源
                string selectedResource = "";
                DateTime closestResourceBase = serviceBase; //在不同资源中寻找最近满足的（与serviceBase相差最小）
                switch (postponeOption)
                {
                    case PostponeOption.后移:
                        closestResourceBase = serviceBase + allowedPostPone;
                        break;
                    case PostponeOption.前移:
                        closestResourceBase = serviceBase - allowedPostPone;
                        break;
                }
                string closestResource = resourceCode;                //在不同资源中寻找最近满足的（与serviceBase相差最小）

                IEnumerable<string> qResource = resources.Cast<string>();
                //if (resourceCode != "")
                //    qResource = qResource.Where(i => i == resourceCode);

                foreach (string searchResourceCode in resources)
                {
                    DateTime dtResourceBase = serviceBase;
                    List<DecomposeConsume> resourceDeConsumes = serviceDeConsumes;
                    bool resourceOK = SearchResourceBase(resourceDeConsumes, searchResourceCode, targetCode, ref dtResourceBase, postponeOption, allowedPostPone, dsSampleCase);
                    if (resourceOK)
                    {
                        TimeSpan servicePostpone = Func.TimeOP.TimesSpanABS(serviceBase - searchBase);
                        if ((servicePostpone > TimeSpan.Zero && minServicePostPone > TimeSpan.Zero &&
                             servicePostpone < minServicePostPone)
                            || servicePostpone > TimeSpan.Zero && minServicePostPone == TimeSpan.Zero)
                            minServicePostPone = servicePostpone;

                        if (Func.TimeOP.TimesSpanABS(dtResourceBase - serviceBase) < Func.TimeOP.TimesSpanABS(closestResourceBase - serviceBase))
                        {
                            //if (serviceCode == "YK")
                            //    dtSampleCaseRow.CaseLogOP.WriteMessage("更新最近：" + dtResourceBase + " - " + resourceCode);
                            closestResourceBase = dtResourceBase;
                            closestResource = searchResourceCode;
                        }
                        serviceOK = true; //有一个资源的当前服务可用即可
                    }
                }

                serviceBase = closestResourceBase;
                selectedResource = closestResource;
                if (serviceOK)
                {
                    //if (serviceCode == "YK")
                    //    dtSampleCaseRow.CaseLogOP.WriteMessage("本服务最近：" + selectedResource + " - " + serviceBase + " - " + selectedResource);
                    foreach (DecomposeConsume serviceConsume in serviceDeConsumes)
                    {
                        serviceConsume.资源代号 = selectedResource;
                    }
                }

                #endregion
            }
            minServicePostPone = Func.TimeOP.TimesSpanABS(serviceBase - searchBase);
            return serviceOK;
        }

        private static bool SearchResourceBase(List<DecomposeConsume> deConsumes,
            string resourceCode, string targetCode,
            ref DateTime dtBase,
            PostponeOption postponeOption, TimeSpan allowedPostPone,
            DataSetSampleCase dsSampleCase)
        {
            //if(resourceCode=="YK")
            //    dtSampleCaseRow.CaseLogOP.WriteMessage("----资源：" + resourceCode + "----" + dtBase);

            TimeSpan TotalPostpone =TimeSpan.Zero;
            Envelope RemainEnvelope = new Envelope();   //当前剩余Envelope
            Envelope ConsumeEnvelope = new Envelope();  //当前消耗Envelope

            foreach (DecomposeConsume deconsume in deConsumes) //累加消耗
            {
                deconsume.绝对基础 = dtBase; //试排基准移到当前位置

                EnvelopeItem consumeEnvelopItem = new EnvelopeItem(deconsume);
                RemainEnvelope.AddRemain(dsSampleCase.dtRemain, consumeEnvelopItem, resourceCode, targetCode);
                ConsumeEnvelope.AddEnvelopeItem(consumeEnvelopItem);
            }

            //WriteTraceCompare(resourceCode, dtSampleCaseRow, RemainEnvelope, ConsumeEnvelope);
            TimeSpan PostponeTS = TimeSpan.Zero;
            while (!Envelope.IsGreaterThan(RemainEnvelope, ConsumeEnvelope, out PostponeTS))
            {
                if (PostponeTS > TimeSpan.Zero)
                {
                    switch (postponeOption)
                    {
                        case  PostponeOption.前移:
                            dtBase -= PostponeTS;
                            TotalPostpone += PostponeTS;
                            break;
                        case PostponeOption.后移:
                            dtBase += PostponeTS;
                            TotalPostpone += PostponeTS;
                            break;
                        case PostponeOption.不顺移:
                            return false;
                        default:
                            return false;
                    }

                    if (TotalPostpone > allowedPostPone)
                    {
                        return false;
                    }

                    //重建
                    RemainEnvelope = new Envelope(); //相关的Remain Envelope
                    ConsumeEnvelope = new Envelope(); //相关的消耗Envelope

                    foreach (DecomposeConsume deconsume in deConsumes)
                    {
                        deconsume.绝对基础 = dtBase;
                        EnvelopeItem consumeEnvelopItem = new EnvelopeItem(deconsume);
                        RemainEnvelope.AddRemain(dsSampleCase.dtRemain, consumeEnvelopItem, resourceCode,targetCode);
                        ConsumeEnvelope.AddEnvelopeItem(consumeEnvelopItem);
                    }

                    //WriteTraceCompare(resourceCode, dtSampleCaseRow, RemainEnvelope, ConsumeEnvelope);

                }
                else
                {
                    //if (resourceCode == "YK")
                    //    dtSampleCaseRow.CaseLogOP.WriteMessage("----资源：" + resourceCode + " " + false + "----" + dtBase);

                    return false;
                }
            }

            //if (resourceCode=="YK")
            //    dtSampleCaseRow.CaseLogOP.WriteMessage("----资源：" + resourceCode + " " + true + "----" + dtBase);

            return true;
        }

        private static void WriteTraceCompare(string resourceCode, DataSetBlackStar.dtSampleCaseRow SampleCase, Envelope RemainEnvelope,
            Envelope ConsumeEnvelope)
        {
            if (resourceCode=="YK")
            {
                foreach (string key in RemainEnvelope.ServiceItems.Keys)
                {
                    SampleCase.CaseLogOP.WriteMessage(key + "剩余：");
                    foreach (EnvelopeItem item in RemainEnvelope.ServiceItems[key])
                    {
                        SampleCase.CaseLogOP.WriteMessage("开始：" + item.Start + "，结束" + item.End);
                    }
                }

                foreach (string key in ConsumeEnvelope.ServiceItems.Keys)
                {
                    SampleCase.CaseLogOP.WriteMessage(key + "消耗：");
                    foreach (EnvelopeItem item in ConsumeEnvelope.ServiceItems[key])
                    {
                        SampleCase.CaseLogOP.WriteMessage("开始：" + item.Start + "，结束" + item.End);
                    }
                }

                TimeSpan ts = TimeSpan.Zero;

                SampleCase.CaseLogOP.WriteMessage("比判结果：" + Envelope.IsGreaterThan(RemainEnvelope, ConsumeEnvelope, out ts));
            }
        }

        
    }
}
