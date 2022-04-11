using BlackStar.Model;
using BlackStar.Model.Interfaces;
using BlackStar.Rules.ListAttributes;
using System;
using System.Collections.Generic;

namespace CustomActionCallingRule;

public partial class RuleLib踏歌
{
    private DataSetBlackStar.dtSampleCaseRow SampleCase;
    private SampleCaseLogOP CaseLogOP
    {
        get
        {
            return this.SampleCase.CaseLogOP;
        }
    }

    public RuleLib踏歌(DataSetBlackStar.dtSampleCaseRow samplecase)
    {
        this.SampleCase = samplecase;   //传递算例数据集
    }
}
