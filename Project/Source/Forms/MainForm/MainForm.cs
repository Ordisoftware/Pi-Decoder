﻿/// <license>
/// This file is part of Ordisoftware Hebrew Pi.
/// Copyright 2025 Olivier Rogier.
/// See www.ordisoftware.com for more information.
/// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
/// If a copy of the MPL was not distributed with this file, You can obtain one at
/// https://mozilla.org/MPL/2.0/.
/// If it is not possible or desirable to put the notice in a particular file,
/// then You may include the notice in a location(such as a LICENSE file in a
/// relevant directory) where a recipient would be likely to look for such a notice.
/// You may add additional accurate notices of copyright ownership.
/// </license>
/// <created> 2025-01 </created>
/// <edited> 2025-01 </edited>
namespace Ordisoftware.Hebrew.Pi;

/// <summary>
/// Provides application's main form.
/// </summary>
/// <seealso cref="T:System.Windows.Forms.Form"/>
partial class MainForm : Form
{

  private PiDecimalsSize PiDecimalsSize;

  private string DatabaseFilePath;

  private string SQLiteTempDir = @"D:\";

  private int SQLiteCacheSize;

  internal SQLiteNetORM DB { get; private set; }

  private string GetSelectedFileName(string extension)
    => $"{nameof(Pi.PiDecimalsSize)} {PiDecimalsSize.ToString().Replace("_", "-")}{extension}";

  #region Singleton

  /// <summary>
  /// Indicates the singleton instance.
  /// </summary>
  static internal MainForm Instance { get; private set; }

  /// <summary>
  /// Static constructor.
  /// </summary>
  static MainForm()
  {
    Instance = new MainForm();
  }

  #endregion

  public MainForm()
  {
    InitializeComponent();
    DoConstructor();
    ColumnIteration.HeaderText = "Itération";
    ColumnRepeatedCount.HeaderText = "Nb de répétés";
    ColumnMaxOccurences.HeaderText = "Max occurrences";
    ColumnRemainingRate.HeaderText = "Taux de restants";
    ColumnElapsedCounting.HeaderText = "Durée comptage";
    ColumnElapsedAdditionning.HeaderText = "Durée addition";
  }

  private void MainForm_Load(object sender, EventArgs e)
  {
    DoFormLoad(sender, e);
    InitializeListBoxCacheSize();
    TimerMemory_Tick(null, null);
  }

  const ulong MemorySizeInKiB = 1024;
  const ulong MemorySizeInMiB = MemorySizeInKiB * 1024;
  const ulong MemorySizeInGiB = MemorySizeInMiB * 1024;

  private void InitializeListBoxCacheSize()
  {
    int memTotal = (int)( SystemManager.TotalVisibleMemoryValue / MemorySizeInGiB );
    int memFree = (int)( SystemManager.PhysicalMemoryFreeValue / MemorySizeInGiB );
    int indexList = 0;
    for ( int indexStep = 0; indexStep < memTotal * 70 / 100; indexStep += 2 )
    {
      SelectDbCache.Items.Add(indexStep);
      if ( indexStep <= memFree ) indexList++;
    }
    SelectDbCache.SelectedIndex = indexList / 2;
  }

  private void MainForm_Shown(object sender, EventArgs e)
  {
    DoFormShown(sender, e);
  }

  private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
  {
    DoFormClosing(sender, e);
  }

  private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
  {
    DoFormClosed(sender, e);
  }

  private void ActionExit_Click(object sender, EventArgs e)
  {
    Close();
  }

  private void ActionExit_MouseUp(object sender, MouseEventArgs e)
  {
    if ( e.Button == MouseButtons.Right )
      ActionExit_Click(ActionExit, null);
  }

  internal void EditScreenPosition_Click(object sender, EventArgs e)
  {
    DoScreenPosition(sender, e);
  }

  private void ActionResetWinSettings_Click(object sender, EventArgs e)
  {
    if ( DisplayManager.QueryYesNo(SysTranslations.AskToRestoreWindowPosition.GetLang()) )
      Settings.RestoreMainForm();
  }

  private void ActionShowKeyboardNotice_Click(object sender, EventArgs e)
  {
    Globals.KeyboardShortcutsNotice?.Popup();
  }

  internal void EditDialogBoxesSettings_CheckedChanged(object sender, EventArgs e)
  {
    Settings.SoundsEnabled = EditSoundsEnabled.Checked;
    DisplayManager.AdvancedFormUseSounds = EditSoundsEnabled.Checked;
    DisplayManager.FormStyle = EditUseAdvancedDialogBoxes.Checked
      ? MessageBoxFormStyle.Advanced
      : MessageBoxFormStyle.System;
    DisplayManager.IconStyle = DisplayManager.FormStyle switch
    {
      MessageBoxFormStyle.System => EditSoundsEnabled.Checked
                                    ? MessageBoxIconStyle.ForceInformation
                                    : MessageBoxIconStyle.ForceNone,
      MessageBoxFormStyle.Advanced => MessageBoxIconStyle.ForceInformation,
      _ => throw new AdvNotImplementedException(DisplayManager.FormStyle),
    };
  }

  private void EditShowSuccessDialogs_CheckStateChanged(object sender, EventArgs e)
  {
    Settings.ShowSuccessDialogs = EditShowSuccessDialogs.Checked;
    DisplayManager.ShowSuccessDialogs = EditShowSuccessDialogs.Checked;
  }

  private void ActionPreferences_Click(object sender, EventArgs e)
  {
    bool temp = Globals.IsReadOnly;
    try
    {
      Globals.IsReadOnly = true;
      //PreferencesForm.Run();
      InitializeSpecialMenus();
      InitializeDialogsDirectory();
    }
    catch ( Exception ex )
    {
      ex.Manage();
    }
    finally
    {
      Globals.IsReadOnly = temp;
    }
  }

  private void ActionViewDecode_Click(object sender, EventArgs e)
  {
    SetView(ViewMode.Decode);
  }

  private void ActionViewGrid_Click(object sender, EventArgs e)
  {
    SetView(ViewMode.Grid);
  }

  private void ActionViewManage_Click(object sender, EventArgs e)
  {
    SetView(ViewMode.Manage);
  }

  public void ActionWebCheckUpdate_Click(object sender, EventArgs e)
  {
    var lastdone = Settings.CheckUpdateLastDone;
    bool exit = WebCheckUpdate.Run(ref lastdone,
                                   Settings.CheckUpdateAtStartupDaysInterval,
                                   e is null,
                                   Settings.CheckUpdateAtStartup);
    Settings.CheckUpdateLastDone = lastdone;
    if ( exit ) Close();
  }

  public void ActionViewLog_Click(object sender, EventArgs e)
  {
    DebugManager.TraceForm.Popup();
  }

  public void ActionViewStats_Click(object sender, EventArgs e)
  {
    StatisticsForm.Run();
  }

  private void ActionDatabaseSetCacheSize_Click(object sender, EventArgs e)
  {
    // TODO
  }

  private void TimerMemory_Tick(object sender, EventArgs e)
  {
    DoTimerMemory();
  }

  private void TimerBatch_Tick(object sender, EventArgs e)
  {
    DoTimerBatch();
  }

  private void ActionDbOpen_Click(object sender, EventArgs e)
  {
    DoActionDbOpen(Path.Combine(Globals.DatabaseFolderPath, GetSelectedFileName(Globals.DatabaseFileExtension)));
  }

  private void ActionDbClose_Click(object sender, EventArgs e)
  {
    DoActionDbClose();
  }

  private void ActionStop_Click(object sender, EventArgs e)
  {
    DoActionStop();
  }

  private void ActionPauseContinue_Click(object sender, EventArgs e)
  {
    DoActionPauseContinue();
  }

  private void SelectFileName_SelectedIndexChanged(object sender, EventArgs e)
  {
    PiDecimalsSize = (PiDecimalsSize)SelectFileName.SelectedItem;
    UpdateButtons();
  }

  private void SelectDbCache_SelectedIndexChanged(object sender, EventArgs e)
  {
    SQLiteCacheSize = (int)SelectDbCache.SelectedItem * (int)MemorySizeInMiB;
    SetDbCache();
  }

  private void SetDbCache()
  {
    DB?.SetCacheSize(SQLiteCacheSize);
  }

  private void GridIterations_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
  {
    if ( e.Value is null )
      e.Value = "?";
    else
    {
      if ( e.ColumnIndex == ColumnRepeatedCount.Index )
        e.Value = ( (long)e.Value ).ToString("N0");
      else
      if ( e.ColumnIndex == ColumnRemainingRate.Index )
        e.Value = ( (double)e.Value ).ToString("0.00") + " %";
      else
      if ( e.ColumnIndex == ColumnElapsedCounting.Index || e.ColumnIndex == ColumnElapsedAdditionning.Index )
      {
        var value = (TimeSpan)e.Value;
        if ( value != TimeSpan.Zero )
          e.Value = ( value ).AsReadable();
      }
    }
  }

  private void ActionCreateData_Click(object sender, EventArgs e)
  {
    //if ( !DisplayManager.QueryYesNo("Empty and create data?") ) return;
    DoBatchAsync(() => DoActionPopulateAsync(Path.Combine(Globals.DocumentsFolderPath, GetSelectedFileName(".txt"))));
  }

  private void ActionNormalize_Click(object sender, EventArgs e)
  {
    //if ( !DisplayManager.QueryYesNo("Start reducing repeating motifs?") ) return;
    DoBatchAsync(() => DoActionNormalizeAsync());
  }

  private void ActionCreateIndex_Click(object sender, EventArgs e)
  {
    //if ( DisplayManager.QueryYesNo(AppTranslations.AskToCreateIndexOnMotif) )
    DoBatchAsync(() => DoCreateIndexAsync(), false);
  }

  private void GridIterations_Leave(object sender, EventArgs e)
  {
    GridIterations.ClearSelection();
  }

  //private void Grid_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
  //{
  //  using var brush = new SolidBrush(Grid.RowHeadersDefaultCellStyle.ForeColor);
  //  e.Graphics.DrawString(( e.RowIndex + 1 ).ToString(),
  //                        e.InheritedRowStyle.Font,
  //                        brush,
  //                        e.RowBounds.Location.X + 10,
  //                        e.RowBounds.Location.Y + 4);
  //}

  //private void Grid_KeyDown(object sender, KeyEventArgs e)
  //{
  //  if ( e.Control && e.KeyCode == Keys.C && Grid.SelectedCells.Count > 0 )
  //  {
  //    var builder = new StringBuilder();
  //    foreach ( DataGridViewCell cell in Grid.SelectedCells )
  //    {
  //      builder.Append(cell.Value.ToString());
  //      if ( cell.ColumnIndex == Grid.Columns.Count - 1 )
  //        builder.AppendLine();
  //      else
  //        builder.Append("\t");
  //    }
  //    Clipboard.SetText(builder.ToString());
  //  }
  //}

  //private void CreateDataTable()
  //{
  //  try
  //  {
  //    var dataTable = new DataTable();
  //    var command = DB.CreateCommand("SELECT * FROM Decuplets LIMIT 1000000 OFFSET 0");
  //    var list = command.ExecuteQuery<DecupletRow>();
  //    if ( list.Count > 0 )
  //    {
  //      foreach ( var prop in typeof(DecupletRow).GetProperties() )
  //        dataTable.Columns.Add(prop.Name);
  //      foreach ( var row in list )
  //      {
  //        var dataRow = dataTable.NewRow();
  //        foreach ( var prop in typeof(DecupletRow).GetProperties() )
  //          dataRow[prop.Name] = prop.GetValue(row);
  //        dataTable.Rows.Add(dataRow);
  //      }
  //    }
  //    Grid.DataSource = dataTable;
  //  }
  //  catch ( Exception ex )
  //  {
  //    UpdateStatusInfo(ex.Message);
  //  }
  //}

}
