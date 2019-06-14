using System.Text.RegularExpressions;

static class RegexUtility
{
    public static string RemoveQuotationMasks(string sourceString)
    {
        return new Regex("(^\")|(\"$)").Replace(sourceString, "");
    }

    public static void RemoveQuotationMasks(ref string sourceString)
    {
        sourceString = RemoveQuotationMasks(sourceString);
    }

    public static string AddQuotationMasks(string sourceString)
    {
        return "\"" + sourceString + "\"";
    }

    public static void AddQuotationMasks(ref string sourceString)
    {
        sourceString = AddQuotationMasks(sourceString);
    }
}
