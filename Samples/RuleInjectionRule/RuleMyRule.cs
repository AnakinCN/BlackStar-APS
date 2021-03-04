using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackStar.Algorithms;
using BlackStar.Functions;
using BlackStar.Model;
using BlackStar.Rules.ListAttributes;
using BlackStar.USL;

namespace RuleInjection
{
    public partial class RuleLibMyLib
    {
       
        [RuleComments("我的规则")]
        public void RuleMyRule([IsDefaultParameter][ParaComments("显示消息")][ParaDefaultValue("要显示的消息")] string para显示消息,
            Guid ruleID)
        {
            var newRow = this.SampleCase.dsSampleCase.dtEvent.NewdtEventRow();
            newRow.事件名称 = "消息";
            newRow.事件代号 = "Message";
            newRow.明细 = para显示消息;
            newRow.开始 = new DateTime(2020,1,1);
            newRow.结束 = new DateTime(2020, 1, 2);
            newRow.来源规则ID = ruleID;
            newRow.事件ID = Guid.NewGuid();
            this.SampleCase.dsSampleCase.dtEvent.AdddtEventRow(newRow);
            this.CaseLogOP.WriteMessage(para显示消息);
        }
    }
}
