using BlackStar.Model;

namespace RuleInjection
{
    public partial class RuleLibMyLib
    {
        private DataSetBlackStar.dtSampleCaseRow SampleCase;
        private SampleCaseLogOP CaseLogOP
        {
            get
            {
                return this.SampleCase.CaseLogOP;
            }
        }

        public RuleLibMyLib(DataSetBlackStar.dtSampleCaseRow samplecase)
        {
            this.SampleCase = samplecase;   //传递算例数据集
        }
    }
}
