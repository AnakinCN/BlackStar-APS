using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BlackStar.Model;
using BlackStar.Rules;
using BlackStar.Rules.ListAttributes;
using BlackStar.USL;
using System.Threading;

namespace RuleInjection
{
    public partial class RuleLibMyLib
    {
        private DataSetBlackStar.dtSampleCaseRow SampleCase;
        //private USLModel uslModel; //每个USLModel为规则和算例独有，可以访问算例具体内容进行USL Parsing
        //private Arrange Arrange;    //规划功能集
        private SampleCaseLogOP CaseLogOP
        {
            get
            {
                return this.SampleCase.CaseLogOP;
            }
        }

        public RuleLibMyLib(DataSetBlackStar.dtSampleCaseRow samplecase)
        {
            this.SampleCase = samplecase;
            //this.uslModel = new USLModel(samplecase);
            //this.Arrange = new Arrange(this.uslModel);
        }
    }
}
