using Microsoft.VisualStudio.Shell;
using System;

namespace ClangPowerTools.Error
{
  public class ErrorListHandler
  {
    #region Members

    private static ErrorListProvider mErrorListProvider = null;

    #endregion

    public ErrorListHandler(IServiceProvider aServiceProvider)
    {
      if (null == mErrorListProvider)
        mErrorListProvider = new ErrorListProvider(aServiceProvider);
    }

    #region Public Methods

    public void Show() => mErrorListProvider.Show();

    public void Clear() => mErrorListProvider.Tasks.Clear();

    #endregion

  }
}
