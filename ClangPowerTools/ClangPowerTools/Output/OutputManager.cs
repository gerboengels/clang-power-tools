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
    private IVsOutputWindowPane mOutputWindowPane;

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

      mOutputWindowPane = new OutputWindow(mDte).GetPane();
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
          while (true == mErrorParser.FindErrors(messages, out TaskError aError))
          {
            ErrorsOccurred = true;

            messages = mErrorParser.Format(messages, aError.FullMessage);

            string beforErrorMessage = StringExtension.SubstringBefore(messages, aError.FullMessage);
            string afterErrorMessage = StringExtension.SubstringAfter(messages, aError.FullMessage);

            mOutputWindowPane.OutputStringThreadSafe(beforErrorMessage + "\n");

            uint line = 0 < (uint)aError.Line ? (uint)aError.Line - 1 : 0;

            var errorCode = mOutputWindowPane.OutputTaskItemString(aError.FullMessage + "\n", aError.Category,
              VSTASKCATEGORY.CAT_BUILDCOMPILE, string.Empty, (int)aError.BitMap, aError.FilePath, line, aError.Description);

            if (0 != mMessagesBuffer.Count)
              mMessagesBuffer.Clear();

            messages = afterErrorMessage;
          }

          if (kBufferSize <= mMessagesBuffer.Count)
          {
            mOutputWindowPane.OutputStringThreadSafe(mMessagesBuffer[0] + "\n");
            mMessagesBuffer.RemoveAt(0);
          }
        }

      }
      catch (Exception)
      {

      }

    }

    public void SyncWithErrorList()
    {
      mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
      {
        mOutputWindowPane.FlushToTaskList();
      }));
    }

    public void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
      {
        if (null == e.Data)
          return;
        mMessagesBuffer.Add(e.Data);
        ProcessOutput(e.Data);

      }));
    }

    public void OutputDataErrorReceived(object sender, DataReceivedEventArgs e)
    {
      mDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
      {
        if (null == e.Data)
          return;
        mMessagesBuffer.Add(e.Data);
        ProcessOutput(e.Data);
      }));
    }

    #endregion

  }
}
