using System.Text.RegularExpressions;
using BlackStar.Model.Interfaces;

namespace USLInjection;

public class MyInjectionClass
{
    
    public static void MatchMySymbol(ref USLMatchResult result, List<string> paraList)
    {
        string pattern = @"注入数据\d+";
        Match m = Regex.Match(result.InputString, pattern, RegexOptions.IgnoreCase);
        if (m.Success)
        {
            result.Success = true;
            result.MatchContent = m.Value.Trim();
            result.MatchLengthNoSpace = m.Value.Trim().Length;
            result.MatchLengthSpace = m.Length;
        }
        else
        {
            result.Success = false;
        }
    }
}
