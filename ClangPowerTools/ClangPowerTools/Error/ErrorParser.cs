using System;
using System.Text.RegularExpressions;

namespace ClangPowerTools
{
  public class ErrorParser
  {
    #region Members

    private const string kCompileErrorsRegex = @"(.\:\\[ \S+\\\/.]*[c|C|h|H|cpp|CPP|cc|CC|cxx|CXX|c++|C++|cp|CP])(\r\n|\r|\n| |:)*(\d+)(\r\n|\r|\n| |:)*(\d+)(\r\n|\r|\n| |:)*(error|note|warning)[^s](\r\n|\r|\n| |:)*(?<=[:|\r\n|\r|\n| ])(.*?)(?=[\[|\r\n|\r|\n])(.*)";

    #endregion

    #region Public Methods

    public bool FindErrors(string aMessages, out TaskError aError)
    {
      Regex regex = new Regex(kCompileErrorsRegex);
      Match matchResult = regex.Match(aMessages);
      aError = null;

      if (!matchResult.Success)
        return false;

      var groups = matchResult.Groups;
      string messageDescription = groups[9].Value;

      if (string.IsNullOrWhiteSpace(messageDescription))
        return false;

      string path = groups[1].Value;
      int.TryParse(groups[3].Value, out int line);

      string categoryAsString = groups[7].Value;
      EnvDTE.vsTaskPriority category = FindErrorCategory(ref categoryAsString);

      string clangTidyChecker = groups[10].Value;
      string fullMessage = ConstructFullErrorMessage(path, line, categoryAsString, clangTidyChecker, messageDescription);

      messageDescription = messageDescription.Insert(0, ErrorParserConstants.kClangTag);
      aError = new TaskError(path, line, category, fullMessage, messageDescription);

      return true;
    }

    private EnvDTE.vsTaskPriority FindErrorCategory(ref string aCategoryAsString)
    {
      EnvDTE.vsTaskPriority category;

      switch (aCategoryAsString)
      {
        case ErrorParserConstants.kErrorTag:
          category = EnvDTE.vsTaskPriority.vsTaskPriorityHigh;
          aCategoryAsString = ErrorParserConstants.kErrorTag;
          break;
        case ErrorParserConstants.kWarningTag:
          category = EnvDTE.vsTaskPriority.vsTaskPriorityMedium;
          aCategoryAsString = ErrorParserConstants.kWarningTag;
          break;
        default:
          category = EnvDTE.vsTaskPriority.vsTaskPriorityLow;
          aCategoryAsString = ErrorParserConstants.kMessageTag;
          break;
      }
      return category;
    }

    private string ConstructFullErrorMessage(string aPath, int aLine, string aCategoryAsString, string aClangTidyChecker, string aDescription)
    {
      return string.Format("{0}({1}): {2}{3}: {4}", aPath, aLine, aCategoryAsString,
        (true == string.IsNullOrWhiteSpace(aClangTidyChecker) ? string.Empty : " " + aClangTidyChecker),
        aDescription);
    }

    public string Format(string aMessages, string aReplacement)
    {
      Regex regex = new Regex(kCompileErrorsRegex);
      return regex.Replace(aMessages, aReplacement);
    }

    public bool LlvmIsMissing(string aMessages)
    {
      return aMessages.Contains(ErrorParserConstants.kCompileClangMissingFromPath) ||
        aMessages.Contains(ErrorParserConstants.kTidyClangMissingFromPath);
    }

    #endregion

  }
}
