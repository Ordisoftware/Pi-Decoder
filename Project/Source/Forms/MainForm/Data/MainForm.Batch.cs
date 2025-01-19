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

using Equin.ApplicationFramework;

/// <summary>
/// Provides application's main form.
/// </summary>
/// <seealso cref="T:System.Windows.Forms.Form"/>
partial class MainForm
{

  private Task BatchTask;
  private bool CanForceTerminateBatch;

  private void LoadIterationGrid()
  {
    void update() => GridIterations.DataSource = new BindingListView<IterationRow>(DB.Table<IterationRow>().ToList());
    if ( GridIterations.InvokeRequired )
      GridIterations.Invoke(update);
    else
      update();
  }

  private void DoActionDbOpen(string path)
  {
    DatabaseFilePath = path;
    LabelTitleCenter.Text = Path.GetFileName(path);
    DB = new SQLiteNetORM(path);
    DB.SetTempDir(SQLiteTempDir);
    DB.SetJournal(false);
    DB.SetSynchronous(false);
    DB.CreateTable<DecupletRow>();
    DB.CreateTable<IterationRow>();
    SetDbCache();
    LoadIterationGrid();
    UpdateButtons();
    TimerMemory_Tick(null, null);
  }

  private void DoActionDbClose()
  {
    if ( DB is null ) return;
    DB.Close();
    DB.Dispose();
    DB = null;
    GridIterations.DataSource = null;
    UpdateButtons();
    TimerMemory_Tick(null, null);
  }

  private async Task DoBatch(Action action)
  {
    try
    {
      ClearStatusBar();
      SetBatchState(true);
      UpdateButtons();
      TimerBatch_Tick(null, null);
      TimerBatch.Enabled = true;
      await Task.Run(async () => action());
    }
    finally
    {
      TimerBatch.Enabled = false;
      Globals.ChronoBatch.Stop();
      Globals.ChronoSubBatch.Stop();
      SetBatchState(false);
      UpdateButtons();
    }
  }

  private void SetBatchState(bool active, bool interruptible = true)
  {
    Globals.IsInBatch = active;
    Globals.PauseRequired = false;
    Globals.CancelRequired = false;
    Globals.CanCancel = interruptible;
    Globals.CanPause = interruptible;
  }

  [DllImport("e_sqlite3", CallingConvention = CallingConvention.Cdecl)]
  public static extern void sqlite3_interrupt(IntPtr db);

  private void DoActionStop()
  {
    Globals.CancelRequired = true;
    if ( CanForceTerminateBatch )
      sqlite3_interrupt(DB.Handle.DangerousGetHandle());
  }

  private void DoActionPauseContinue()
  {
    Globals.PauseRequired = !Globals.PauseRequired;
    UpdateButtons();
    if ( Globals.PauseRequired )
      Globals.ChronoBatch.Stop();
    else
      Globals.ChronoBatch.Start();
  }

  private async Task<bool> CheckIfBatchCanContinueAsync()
  {
    if ( Globals.CancelRequired ) return false;
    while ( Globals.PauseRequired && !Globals.CancelRequired )
      await Task.Delay(500);
    return true;
  }

}
