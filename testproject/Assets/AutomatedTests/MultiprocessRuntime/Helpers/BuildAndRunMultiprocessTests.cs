using System;
using System.IO;
using MLAPI;
using MLAPI.MultiprocessRuntimeTests;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
#endif
using UnityEngine;

/// <summary>
/// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
/// when trying to build from Setup() steps in tests.
/// </summary>
public class BuildAndRunMultiprocessTests : MonoBehaviour
{

    public const string BuildAndExecuteMenuName = "MLAPI Tests/Build - Execute multiprocess tests #%t";
    public static string buildPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds/MultiprocessTestBuild");

#if UNITY_EDITOR
    [MenuItem(BuildAndExecuteMenuName)]
    public static void BuildAndExecute()
    {
        var shouldContinue = Build(); // todo try using     yield return new EnterPlayMode(); from edit mode tests so we can
        if (shouldContinue)
        {
            Execute(); // todo this is broken?
        }
        else
        {
            throw new Exception("Build failed to create!!");
        }
    }

    [MenuItem("MLAPI Tests/No Build - Execute multiprocess tests %t")]
    public static void ExecuteNoBuild()
    {
        Execute();
    }

    [MenuItem("MLAPI Tests/Build Test Player #t")]
    public static void BuildNoExecute()
    {
        var success = Build();
        if (!success)
        {
            throw new Exception("Build failed!");
        }
    }


    [MenuItem("MLAPI Tests/Delete Performance Build")]
    public static void DeleteBuild()
    {
#if UNITY_EDITOR_OSX
        var toDelete = buildPath + ".app";
        if (Directory.Exists(toDelete))
        {
            Directory.Delete(toDelete, recursive: true);
        }
        else
        {
            Debug.Log($"directory {toDelete} doesn't exists");
        }
#elif UNITY_EDITOR_WIN
        var exePath = Path.Combine(buildPath, $"{PlayerSettings.productName}.exe");
        if (File.Exists(exePath))
        {
            File.Delete(exePath);
        }
        else
        {
            Debug.Log($"exe {exePath} doesn't exists");
        }
#else
        throw new NotImplementedException();
#endif
    }

    /// <summary>
    /// To run these from the command line, call
    /// runMultiplayerTests.sh
    ///
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static void Execute()
    {
        // create builds from the test itself
        StartMainTestNodeInEditor();
        // todo this doesn't work from the command line. if -executeMethod is used, EditorApplication doesn't update
        // however, calling from the commandline -runTests with platform playmode does work. Will need to figure out
        // what's the difference between the two and how to get EditorApplication to run outside of -runTests
        // right now, can just run both executeMethod (which will launch the players) and -runTests one after the other to
        // get a successful test.
    }

    /// <summary>
    /// Needs a separate build than the standalone test builds since we don't want the player to try to connect to the editor to do test
    /// reporting. We only want to main node to do that, worker nodes should be dumb
    /// </summary>
    /// <returns></returns>
    public static bool Build()
    {
        // Save standalone build path to file
        var f = File.CreateText(Path.Combine(Application.streamingAssetsPath, TestCoordinator.buildInfoFileName));
        f.Write(buildPath);
        f.Close();

        // var buildPath = Application.streamingAssetsPath;
        // deleting so we don't endup testing on outdated builds
        DeleteBuild();
#if UNITY_EDITOR_OSX
        var buildTarget = BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_WIN
        // todo test on windows
        var buildTarget = BuildTarget.StandaloneWindows;
#else
        throw new NotImplementedException("Building for this platform is not supported");
#endif
        var buildOptions = BuildOptions.None;
        buildOptions |= BuildOptions.IncludeTestAssemblies;
        buildOptions |= BuildOptions.StrictMode;
        // buildOptions |= BuildOptions.Development;
        // buildOptions |= BuildOptions.AllowDebugging; // enable this if you want to debug your players. Your players
        // will have more connection permission popups when launching though

        buildOptions &= ~BuildOptions.AutoRunPlayer;
        var buildReport = BuildPipeline.BuildPlayer(
            new string[] { $"Assets/Scenes/{BaseMultiprocessTests.mainSceneName}.unity" },
            buildPath,
            buildTarget,
            buildOptions);

        return buildReport.summary.result == BuildResult.Succeeded;
    }

    private static void StartMainTestNodeInEditor()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();

        testRunnerApi.Execute(new ExecutionSettings()
            {
                filters = new Filter[]
                {
                    new Filter()
                    {
                        categoryNames = new [] {MultiprocessTests.multiprocessCategoryName},
                        testMode = TestMode.PlayMode
                    },
                },
            }
        );
    }
#endif

}
