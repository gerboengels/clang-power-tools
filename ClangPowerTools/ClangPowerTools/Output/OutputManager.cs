using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
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

    private int kBufferSize = 5;
    private List<string> mMessagesBuffer = new List<string>();

    private ErrorParser mErrorParser = new ErrorParser();

    private bool mMissingLlvm = false;
    private HashSet<TaskError> mErrors = new HashSet<TaskError>();

    private List<string> mPCHPaths = new List<string>();

    #endregion

    #region Properties

    public bool MissingLlvm => mMissingLlvm;

    public List<string> Buffer => mMessagesBuffer;

    public bool EmptyBuffer => mMessagesBuffer.Count == 0;

    public HashSet<TaskError> Errors => mErrors;

    public bool HasErrors => 0 != mErrors.Count;

    public List<string> PCHPaths => mPCHPaths;

    #endregion

    #region Constructor

    public OutputManager(DTE2 aDte)
    {
      mDte = aDte;
      mDispatcher = HwndSource.FromHwnd((IntPtr)mDte.MainWindow.HWnd).RootVisual.Dispatcher;
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
        if (mErrorParser.LlvmIsMissing(aMessage))
        {
          mMissingLlvm = true;
        }
        else if (!mMissingLlvm)
        {
          string messages = String.Join("\n", mMessagesBuffer);
          if (mErrorParser.FindErrors(messages, out TaskError aError, out string fullMessage))
          {
            messages = mErrorParser.Format(messages, aError.FullMessage);
            var pane = mDte.ToolWindows.OutputWindow.ActivePane;

            //string[] arrStr = mErrorParser.Split(messages, aError.FullMessage);


            // var pane2 = pane as IVsOutputWindowPane2;

            //pane.OutputTaskItemString(messages + "\n", aError.Type, EnvDTE.vsTaskCategories.vsTaskCategoryComment,
            //  EnvDTE.vsTaskIcon.vsTaskIconComment, null, 0, null, false);

            string beforErrorMessage = StringExtensions.SubstringBefore(messages, aError.FullMessage);
            string afterErrorMessage = StringExtensions.SubstringAfter(messages, aError.FullMessage);

            //string beforErrorMessage = messages.Substring(0, messages.IndexOf(aError.FullMessage));
            //string afterErrorMessage = messages.Substring(messages.IndexOf(aError.FullMessage) + aError.FullMessage.Length);

            pane.OutputTaskItemString(beforErrorMessage + "\n", aError.Type, EnvDTE.vsTaskCategories.vsTaskCategoryComment,
              EnvDTE.vsTaskIcon.vsTaskIconComment, null, 0, null, false);


            pane.OutputTaskItemString(aError.FullMessage + "\n", aError.Type, EnvDTE.vsTaskCategories.vsTaskCategoryBuildCompile,
              EnvDTE.vsTaskIcon.vsTaskIconCompile, aError.FilePath, aError.Line, aError.Message, true);

            pane.OutputTaskItemString(afterErrorMessage + "\n", aError.Type, EnvDTE.vsTaskCategories.vsTaskCategoryComment,
           EnvDTE.vsTaskIcon.vsTaskIconComment, null, 0, null, false);


            pane.ForceItemsToTaskList();

            //pane.OutputTaskItemStringEx(aError.FullMessage, VSTASKPRIORITY.TP_HIGH , VSTASKCATEGORY.CAT_BUILDCOMPILE, null, 
            //  (int)_vstaskbitmap.BMP_COMPILE, aError.FilePath, (uint)aError.Line, 0, null, aError.Message, VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_LookupF1.ToString());

            //AddMessage(messages);
            mMessagesBuffer.Clear();
          }
          else if (kBufferSize <= mMessagesBuffer.Count)
          {
            AddMessage(mMessagesBuffer[0]);
            mMessagesBuffer.RemoveAt(0);
          }
        }
      }
      catch (Exception ex)
      {
        
      }
    }

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
