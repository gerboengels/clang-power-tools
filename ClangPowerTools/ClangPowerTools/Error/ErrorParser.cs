using System.Text.RegularExpressions;

namespace ClangPowerTools
{
  public class ErrorParser
  {
    #region Members

    private const string kCompileErrorsRegex = @"(.\:\\[ \S+\\\/.]*[c|C|h|H|cpp|CPP|cc|CC|cxx|CXX|c++|C++|cp|CP])(\r\n|\r|\n| |:)*(\d+)(\r\n|\r|\n| |:)*(\d+)(\r\n|\r|\n| |:)*(error|note|warning)[^s](\r\n|\r|\n| |:)*(?<=[:|\r\n|\r|\n| ])(.*?)(?=[\[|\r\n|\r|\n])(.*)";

    #endregion

    #region Public Methods

    public bool FindErrors(string aMessages, out TaskError aError, out string aFullMessage)
    {
      aFullMessage = string.Empty;

      Regex regex = new Regex(kCompileErrorsRegex);
      Match matchResult = regex.Match(aMessages);
      aError = null;
      if (!matchResult.Success)
        return false;

      var groups = matchResult.Groups;
      string message = groups[9].Value;
      string clangTidyChecker = groups[10].Value;

      if (string.IsNullOrWhiteSpace(message))
        return false;

      string path = groups[1].Value;
      int.TryParse(groups[3].Value, out int line);
      string category = groups[7].Value;

      CategoryAndFullMessageBuilder(category, message, path, line, clangTidyChecker, 
        out EnvDTE.vsTaskPriority taskPriority, out string fullMessage);

      message = message.Insert(0, ErrorParserConstants.kClangTag);
      aError = new TaskError(path, fullMessage, message, line, taskPriority);

      aFullMessage = fullMessage;

      return true;
    }

    private void CategoryAndFullMessageBuilder(string aCategory, string aMessage, string aPath, 
      int aLine, string aClangTidyChecker, out EnvDTE.vsTaskPriority aTaskPriority, out string aFullMessage)
    {
      aFullMessage = $"{aPath}({aLine}): ";

      switch (aCategory)
      {
        case ErrorParserConstants.kErrorTag:
          aTaskPriority = EnvDTE.vsTaskPriority.vsTaskPriorityHigh;
          aFullMessage = $"{aFullMessage}{ErrorParserConstants.kErrorTag}";
          break;
        case ErrorParserConstants.kWarningTag:
          aTaskPriority = EnvDTE.vsTaskPriority.vsTaskPriorityMedium;
          aFullMessage = $"{aFullMessage}{ErrorParserConstants.kWarningTag}";
          break;
        default:
          aTaskPriority = EnvDTE.vsTaskPriority.vsTaskPriorityLow;
          aFullMessage = $"{aFullMessage}{ErrorParserConstants.kMessageTag}";
          break;
      }

      aFullMessage = (true == string.IsNullOrWhiteSpace(aClangTidyChecker) ? $"{aFullMessage}:" : $"{aFullMessage} {aClangTidyChecker}:");
      aFullMessage = $"{aFullMessage}{aMessage}";
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
