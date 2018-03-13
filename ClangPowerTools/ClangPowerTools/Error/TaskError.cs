﻿namespace ClangPowerTools
{
  public class TaskError
  {
    #region Properties

    public string FilePath { get; set; }

    public int Line { get; set; }

    public EnvDTE.vsTaskPriority Category { get; set; }

    public string FullMessage { get; set; }

    public string Description { get; set; }

    #endregion

    #region Constructor

    public TaskError(string aFilePath, int aLine, EnvDTE.vsTaskPriority aCategory,
      string aFullMessage, string aDescription)
    {
      FilePath = aFilePath;
      Line = aLine;
      Category = aCategory;
      FullMessage = aFullMessage;
      Description = aDescription;
    }

    #endregion

    #region Public Methods

    public override bool Equals(object obj)
    {
      var otherObj = obj as TaskError;
      if (null == otherObj)
        return false;

      return FullMessage == otherObj.FullMessage;
    }

    public override int GetHashCode()
    {
      return $"{Line.ToString()}{FilePath}{FullMessage}".GetHashCode();
    }

    #endregion

  }
}
