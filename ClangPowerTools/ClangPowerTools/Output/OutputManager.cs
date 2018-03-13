using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ClangPowerTools
{
  public class OutputManager
  {
    #region Members

    private DTE2 mDte = null;
    private Dispatcher mDispatcher;
    private OutputWindowPane mOutputWindowPane;

    private int kBufferSize = 5;
    private List<string> mMessagesBuffer = new List<string>();

    private ErrorParser mErrorParser = new ErrorParser();

    private bool mMissingLlvm = false;

    private List<string> mPCHPaths = new List<string>();

    #endregion

    #region Properties

    public bool ErrorsOccurred { get; set; }

    public bool MissingLlvm => mMissingLlvm;

    public List<string> Buffer => mMessagesBuffer;

    public bool EmptyBuffer => mMessagesBuffer.Count == 0;


    public List<string> PCHPaths => mPCHPaths;

    #endregion

    #region Constructor

    public OutputManager(DTE2 aDte)
    {
      mDte = aDte;
      mDispatcher = HwndSource.FromHwnd((IntPtr)mDte.MainWindow.HWnd).RootVisual.Dispatcher;

      //mOutputWindowPane = new OutputWindow(mDte).GetPane();

    }

    #endregion

    #region Public Methods

    public void Clear()
    {
      using (OutputWindow outputWindow = new OutputWindow(mDte))
      {
        mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
          outputWindow.Clear();
        }));
      }
    }

    public void Show()
    {
      using (OutputWindow outputWindow = new OutputWindow(mDte))
        outputWindow.Show(mDte);
    }

    public void AddMessage(string aMessage)
    {
      if (String.IsNullOrWhiteSpace(aMessage))
        return;

      using (OutputWindow outputWindow = new OutputWindow(mDte))
      {
        mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
        {
          outputWindow.Write(aMessage);
        }));
      }
    }

    private void ProcessOutput(string aMessage)
    {
      try
      {
        mOutputWindowPane = mDte.ToolWindows.OutputWindow.ActivePane;

        if (mErrorParser.LlvmIsMissing(aMessage))
        {
          mMissingLlvm = true;
        }
        else if (!mMissingLlvm)
        {
          string messages = String.Join("\n", mMessagesBuffer);
          while (true == mErrorParser.FindErrors(messages, out TaskError aError))
          {
            ErrorsOccurred = true;

            messages = mErrorParser.Format(messages, aError.FullMessage);

            string beforErrorMessage = StringExtension.SubstringBefore(messages, aError.FullMessage);
            string afterErrorMessage = StringExtension.SubstringAfter(messages, aError.FullMessage);

           // AddMessage(beforErrorMessage);

            mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
              mOutputWindowPane.OutputTaskItemString(beforErrorMessage, vsTaskPriority.vsTaskPriorityLow, null,
               EnvDTE.vsTaskIcon.vsTaskIconComment, null, 0, null, false);

              mOutputWindowPane.OutputTaskItemString(aError.FullMessage + "\n", aError.Category, EnvDTE.vsTaskCategories.vsTaskCategoryBuildCompile,
                EnvDTE.vsTaskIcon.vsTaskIconCompile, aError.FilePath, aError.Line, aError.Description, true);

              mOutputWindowPane.ForceItemsToTaskList();

            }));

            if (0 != mMessagesBuffer.Count)
              mMessagesBuffer.Clear();

            messages = afterErrorMessage;
          }

          if (kBufferSize <= mMessagesBuffer.Count)
          {
            mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
              mOutputWindowPane.OutputTaskItemString(mMessagesBuffer[0], vsTaskPriority.vsTaskPriorityLow, EnvDTE.vsTaskCategories.vsTaskCategoryComment,
               EnvDTE.vsTaskIcon.vsTaskIconComment, null, 0, null, false);
            }));

            // AddMessage(mMessagesBuffer[0]);
            mMessagesBuffer.RemoveAt(0);
          }
        }
      }
      catch (Exception)
      {

      }

    }

    //public void SyncWithErrorList()
    //{
    //  mOutputWindowPane.ForceItemsToTaskList();
    //}

    public void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      if (null == e.Data)
        return;
      mMessagesBuffer.Add(e.Data);
      ProcessOutput(e.Data);
    }

    public void OutputDataErrorReceived(object sender, DataReceivedEventArgs e)
    {
      if (null == e.Data)
        return;
      mMessagesBuffer.Add(e.Data);
      ProcessOutput(e.Data);
    }

    #endregion

  }
}
