using System.Globalization;

namespace ClangPowerTools
{
  public static class StringExtension
  {
    public static string SubstringAfter(this string source, string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return source;
      }
      CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
      int index = compareInfo.IndexOf(source, value, CompareOptions.Ordinal);
      if (index < 0)
      {
        //No such substring
        return string.Empty;
      }
      return source.Substring(index + value.Length);
    }

    public static string SubstringBefore(this string source, string value)
    {
      if (string.IsNullOrEmpty(value))
      {
        return value;
      }
      CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
      int index = compareInfo.IndexOf(source, value, CompareOptions.Ordinal);
      if (index < 0)
      {
        //No such substring
        return string.Empty;
      }
      return source.Substring(0, index);
    }
  }
}