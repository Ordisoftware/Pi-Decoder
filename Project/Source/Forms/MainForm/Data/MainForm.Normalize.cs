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
partial class MainForm
{

  private async Task DoActionNormalize()
  {
    var chrono = new Stopwatch();
    bool hasError = false;
    try
    {
      CanForceTerminateBatch = true;
      var lastRow = DB.Table<IterationRow>().ToList().LastOrDefault();
      long indexIteration = lastRow?.Iteration + 1 ?? 0;
      long countPrevious = lastRow?.RepeatedCount ?? 0;
      long countCurrent = 1;
      if ( lastRow is not null && countPrevious == 0 ) return;
      Globals.ChronoBatch.Restart();
      UpdateStatusRemaining(AppTranslations.RemainingNAText);
      for ( ; countCurrent > 0; indexIteration++ )
      {
        // Count repeating motifs
        if ( !CheckIfBatchCanContinue().Result ) break;
        UpdateStatusInfo(string.Format(AppTranslations.IterationText, indexIteration, "?"));
        UpdateStatusAction(AppTranslations.CountingText);
        Globals.ChronoSubBatch.Restart();
        var list = GetRepeatingMotifsAndMaxOccurences().Result;
        countCurrent = list.Count;
        var row = new IterationRow
        {
          Iteration = indexIteration,
          RepeatedCount = countCurrent,
          MaxOccurences = list.Any() ? list[0].Occurences : 0
        };
        Globals.ChronoSubBatch.Stop();
        row.ElapsedCount = Globals.ChronoSubBatch.Elapsed;
        UpdateStatusInfo(string.Format(AppTranslations.IterationText, indexIteration, countCurrent));
        UpdateStatusAction(AppTranslations.CountedText);
        if ( indexIteration > 0 && countCurrent > countPrevious )
          if ( !DisplayManager.QueryYesNo(string.Format(AppTranslations.AskStartNextIfMore,
                                                        indexIteration,
                                                        countPrevious,
                                                        countCurrent)) )
          {
            Globals.CancelRequired = true;
            break;
          }
        countPrevious = countCurrent;
        // Add position to repeating motifs
        if ( !CheckIfBatchCanContinue().Result ) break;
        if ( countCurrent > 0 )
        {
          UpdateStatusAction(AppTranslations.UpdatingText);
          Globals.ChronoSubBatch.Restart();
          AddPositionToRepeatingMotifs();
          Globals.ChronoSubBatch.Stop();
          row.ElapsedAddition = Globals.ChronoSubBatch.Elapsed;
        }
        // Insert row and reload grid
        DB.Insert(row);
        GridIterations.Invoke(LoadIterationGrid);
      }
    }
    catch ( Exception ex )
    {
      hasError = true;
      UpdateStatusAction(ex.Message);
    }
    finally
    {
      CanForceTerminateBatch = false;
      if ( !hasError )
        if ( Globals.CancelRequired )
          UpdateStatusAction(AppTranslations.CanceledText);
        else
          UpdateStatusAction(AppTranslations.FinishedText);
    }
  }

}
