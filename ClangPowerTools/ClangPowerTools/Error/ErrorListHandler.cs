using Microsoft.VisualStudio.Shell;
using System;

namespace ClangPowerTools.Error
{
  public class ErrorListHandler
  {
    #region Members

    private static ErrorListProvider mErrorListProvider = null;

    #endregion

    #region Constructor

    public ErrorListHandler(IServiceProvider aServiceProvider)
    {
      if (null == mErrorListProvider)
        mErrorListProvider = new ErrorListProvider(aServiceProvider);
    }

    #endregion

    #region Public Methods

    public void Show()
    {
      mErrorListProvider.ForceShowErrors();
      mErrorListProvider.BringToFront();
    }

    public void Clear()
    {
      mErrorListProvider.Tasks.Clear();
      mErrorListProvider.Refresh();
    }

    #endregion

  }
}
