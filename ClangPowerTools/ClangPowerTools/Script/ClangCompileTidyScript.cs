﻿using ClangPowerTools.DialogPages;
using EnvDTE;
using EnvDTE80;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ClangPowerTools.Script
{
  public class ClangCompileTidyScript : ScriptBuiler
  {
    #region Members

    private bool mUseTidyFile = false;

    #endregion

    #region Public Methods

    public string GetScript(IItem aItem, string aSolutionPath)
    {
      string containingDirectoryPath = string.Empty;
      string script = $"{ScriptConstants.kScriptBeginning} ''{GetFilePath()}''";

      if (aItem is SelectedProjectItem)
      {
        ProjectItem projectItem = aItem.GetObject() as ProjectItem;
        string containingProject = projectItem.ContainingProject.FullName;
        script = $"{script} {ScriptConstants.kProject} ''{containingProject}'' " +
          $"{ScriptConstants.kFile} ''{projectItem.Properties.Item("FullPath").Value}'' {ScriptConstants.kActiveConfiguration} " +
          $"''{ProjectConfiguration.GetConfiguration(projectItem.ContainingProject)}|{ProjectConfiguration.GetPlatform(projectItem.ContainingProject)}''";
      }
      else if (aItem is SelectedProject)
      {
        Project project = aItem.GetObject() as Project;
        script = $"{script} {ScriptConstants.kProject} ''{project.FullName}'' {ScriptConstants.kActiveConfiguration} " +
          $"''{ProjectConfiguration.GetConfiguration(project)}|{ProjectConfiguration.GetPlatform(project)}''";
      }
      return $"{script} {mParameters} {ScriptConstants.kDirectory} ''{aSolutionPath}'' {ScriptConstants.kLiteral}'";
    }

    public void ConstructParameters(GeneralOptions aGeneralOptions, TidyOptions aTidyOptions, TidyChecks aTidyChecks, 
      TidyCustomChecks aTidyCustomChecks, ClangFormatPage aClangFormat, DTE2 aDTEObj, string aVsEdition, string aVsVersion)
    {
      mParameters = GetGeneralParameters(aGeneralOptions);
      mParameters = null != aTidyOptions ?
        $"{mParameters} {GetTidyParameters(aTidyOptions, aTidyChecks, aTidyCustomChecks)}" : $"{mParameters} {ScriptConstants.kParallel}";

      if (null != aClangFormat && null != aTidyOptions && true == aTidyOptions.Fix && true == aTidyOptions.FormatAfterTidy)
        mParameters = $"{mParameters} {ScriptConstants.kClangFormatStyle} {GetClangFormatParameters(aClangFormat)}";

      mParameters = $"{mParameters} {ScriptConstants.kVsVersion} {aVsVersion} {ScriptConstants.kVsEdition} {aVsEdition}";
    }

    #endregion

    #region Get Parameters Helpers

    //Get the script file path
    protected override string GetFilePath() => Path.Combine(base.GetFilePath(), ScriptConstants.kScriptName);

    private string GetGeneralParameters(GeneralOptions aGeneralOptions)
    {
      string parameters = string.Empty;

      if (null != aGeneralOptions.ClangFlags && 0 < aGeneralOptions.ClangFlags.Length)
      {
        parameters = $"{parameters} {ScriptConstants.kClangFlags} (" +
          $"{string.Format("{0}", aGeneralOptions.TreatWarningsAsErrors ? string.Format("''{0}'',", ScriptConstants.kTreatWarningsAsErrors) : string.Empty)}''" +
          $"{String.Join("'',''", aGeneralOptions.ClangFlags)}'')";
      }

      if (true == aGeneralOptions.Continue)
        parameters = $"{parameters} {ScriptConstants.kContinue}";

      if (true == aGeneralOptions.VerboseMode)
        parameters = $"{parameters} {ScriptConstants.kVerboseMode}";

      if (null != aGeneralOptions.ProjectsToIgnore && 0 < aGeneralOptions.ProjectsToIgnore.Length)
        parameters = $"{parameters} {ScriptConstants.kProjectsToIgnore} (''{String.Join("'',''", aGeneralOptions.ProjectsToIgnore)}'')";

      if (null != aGeneralOptions.FilesToIgnore && 0 < aGeneralOptions.FilesToIgnore.Length)
        parameters = $"{parameters} {ScriptConstants.kFilesToIgnore} (''{String.Join("'',''", aGeneralOptions.FilesToIgnore)}'')";

      if (0 == string.Compare(aGeneralOptions.AdditionalIncludes, ComboBoxConstants.kSystemIncludeDirectories))
        parameters = $"{parameters} {ScriptConstants.kSystemIncludeDirectories}";

      return $"{parameters}";
    }

    private string GetTidyParameters(TidyOptions aTidyOptions, TidyChecks aTidyChecks, TidyCustomChecks aTidyCustomChecks)
    {
      string parameters = string.Empty;

      if (0 == string.Compare(ComboBoxConstants.kTidyFile, aTidyOptions.UseChecksFrom))
      {
        parameters = $"{parameters}{ScriptConstants.kTidyFile}";
        mUseTidyFile = true;
      }
      else if (0 == string.Compare(ComboBoxConstants.kCustomChecks, aTidyOptions.UseChecksFrom))
      {
        if (null != aTidyCustomChecks.TidyChecks && 0 != aTidyCustomChecks.TidyChecks.Length)
          parameters = $",{String.Join(",", aTidyCustomChecks.TidyChecks)}";
      }
      else if (0 == string.Compare(ComboBoxConstants.kPredefinedChecks, aTidyOptions.UseChecksFrom))
      {
        foreach (PropertyInfo prop in aTidyChecks.GetType().GetProperties())
        {
          object[] propAttrs = prop.GetCustomAttributes(false);
          object clangCheckAttr = propAttrs.FirstOrDefault(attr => typeof(ClangCheckAttribute) == attr.GetType());
          object displayNameAttrObj = propAttrs.FirstOrDefault(attr => typeof(DisplayNameAttribute) == attr.GetType());

          if (null == clangCheckAttr || null == displayNameAttrObj)
            continue;

          DisplayNameAttribute displayNameAttr = (DisplayNameAttribute)displayNameAttrObj;
          var value = prop.GetValue(aTidyChecks, null);
          if (Boolean.TrueString != value.ToString())
            continue;
          parameters = $"{parameters},{displayNameAttr.DisplayName}";
        }
      }

      if (string.Empty != parameters)
      {
        parameters = string.Format("{0} ''{1}{2}''",
          (true == aTidyOptions.Fix ? ScriptConstants.kTidyFix : ScriptConstants.kTidy),
          (mUseTidyFile ? "" : "-*"),
          parameters);
      }

      if (false == string.IsNullOrWhiteSpace(aTidyOptions.HeaderFilter))
      {
        parameters = string.Format("{0} {1} ''{2}''", parameters, ScriptConstants.kHeaderFilter,
          ComboBoxConstants.kHeaderFilterMaping.ContainsKey(aTidyOptions.HeaderFilter) ?
          ComboBoxConstants.kHeaderFilterMaping[aTidyOptions.HeaderFilter] : aTidyOptions.HeaderFilter);
      }

      return parameters;
    }

    private string GetClangFormatParameters(ClangFormatPage aClangFormat)
    {
      if (true == string.IsNullOrWhiteSpace(aClangFormat.Style))
        return string.Empty;

      return aClangFormat.Style;
    }

    #endregion


  }
}
