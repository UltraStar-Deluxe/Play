using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UniInject
{
    public class UniInjectMenuItems
    {

        [MenuItem("UniInject/Check current scene &v")]
        public static void CheckCurrentScene()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            int errorCount = CheckingUtils.CheckCurrentScene();

            stopwatch.Stop();
            Debug.Log($"Scene check found {errorCount} issue(s) in {stopwatch.ElapsedMilliseconds} ms.");
        }

    }
}