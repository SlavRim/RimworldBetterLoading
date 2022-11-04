using System;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace BetterLoading
{
    public class ToExecuteWhenFinishedHandler
    {
        private const float TARGET_FPS_DURING_LOAD = 25;
        private const float MAX_FRAME_DURATION = 1f / TARGET_FPS_DURING_LOAD * 10_000_000f;
        private static long lastEnd;

        public static IEnumerator ExecuteToExecuteWhenFinishedTheGoodVersion(List<Action> toExecuteWhenFinished, bool skipLast, Action<Action> actionStartCallback, Action taskFinishedCallback, Action completeCallback)
        {
            lastEnd = DateTime.UtcNow.Ticks;
            if (LongEventHandlerMirror.CurrentlyExecutingToExecuteWhenFinished)
            {
                Log.Warning("[BetterLoading] ToExecuteWhenFinishedHandler already executing.");
            }
            else
            {
                LongEventHandlerMirror.CurrentlyExecutingToExecuteWhenFinished = true;

                if (toExecuteWhenFinished.Count > 0)
                    DeepProfiler.Start("ExecuteToExecuteWhenFinished()");
                
                for (var index = 0; index < toExecuteWhenFinished.Count + (skipLast ? -1 : 0);)
                {
                    do
                    {
                        var action = toExecuteWhenFinished[index];
                        DeepProfiler.Start($"{action.Method.DeclaringType} -> {action.Method}");
                        try
                        {
                            // _currentAction = action;
                            actionStartCallback(action);

                            action();

                            // _numTasksRun++;
                            taskFinishedCallback();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Could not execute post-long-event action. Exception: " + ex);
                        }
                        finally
                        {
                            DeepProfiler.End();
                        }

                        index++;
                        if(index >= toExecuteWhenFinished.Count)
                            break;
                    } while (DateTime.UtcNow.Ticks - lastEnd < MAX_FRAME_DURATION); //Target 30fps

                    yield return null;
                    lastEnd = DateTime.UtcNow.Ticks;
                }

                try
                {
                    if (toExecuteWhenFinished.Count > 0)
                        DeepProfiler.End();

                    // LongEventHandlerMirror.ToExecuteWhenFinished = new List<Action>();
                    LongEventHandlerMirror.CurrentlyExecutingToExecuteWhenFinished = false;
                }
                catch (Exception e)
                {
                    Log.Error("[BetterLoading] Exception finishing up toExecuteWhenFinished! " + e);
                }
                finally
                {
                    completeCallback();
                }
            }
        }
    }
}