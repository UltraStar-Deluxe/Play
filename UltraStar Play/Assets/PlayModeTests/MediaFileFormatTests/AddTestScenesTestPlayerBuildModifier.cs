// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEditor.TestTools;
// using UnityEngine;
// using UnityEngine.TestTools;
//
// [assembly:TestPlayerBuildModifier(typeof(AddTestScenesPlayModeTestBuildModifer))]
// [assembly:PostBuildCleanup(typeof(AddTestScenesPlayModeTestBuildModifer))]
// public class AddTestScenesPlayModeTestBuildModifer : ITestPlayerBuildModifier, IPostBuildCleanup
// {
//     private static List<string> testScenePaths = new()
//     {
//         "Assets/PlayModeTests/MediaFileFormatTests/MediaFileFormatTestScene.unity",
//     };
//
//     public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
//     {
//         List<string> originalScenes = playerOptions.scenes.ToList();
//         List<string> originalScenesAndTestScene = originalScenes
//             .Union(testScenePaths)
//             .ToList();
//
//         playerOptions.scenes = originalScenesAndTestScene.ToArray();
//         return playerOptions;
//     }
//
//     public void Cleanup()
//     {
//         // Unused
//     }
// }
