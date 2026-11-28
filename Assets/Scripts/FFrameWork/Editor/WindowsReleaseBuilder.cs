//Create By Codex for Debug
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FFrameWork.Editor
{
    public static class WindowsReleaseBuilder
    {
        [MenuItem("FFrameWork/Build/Windows Release")]
        public static void BuildWindows()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scene is configured for the release.");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string configuredOutput = Environment.GetEnvironmentVariable("FFRAMEWORK_RELEASE_OUTPUT");
            string outputDirectory = string.IsNullOrWhiteSpace(configuredOutput)
                ? Path.Combine(projectRoot, ".publish-artifacts", "SonoriX-Windows-x64")
                : Path.GetFullPath(configuredOutput);
            Directory.CreateDirectory(outputDirectory);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, "SonoriX.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Windows release failed: {report.summary.result}; errors: {report.summary.totalErrors}");
            }

            CopyIfPresent(projectRoot, outputDirectory, "THIRD_PARTY_NOTICES.md");
            CopyIfPresent(projectRoot, outputDirectory, "RELEASE_README.txt");
            string fmodLicense = Path.Combine(projectRoot, "Assets", "Plugins", "FMOD", "LICENSE.txt");
            if (File.Exists(fmodLicense))
            {
                File.Copy(fmodLicense, Path.Combine(outputDirectory, "FMOD-LICENSE.txt"), true);
            }
            Debug.Log($"Windows release built: {outputDirectory}; size: {report.summary.totalSize} bytes");
        }

        private static void CopyIfPresent(string sourceRoot, string destinationRoot, string fileName)
        {
            string source = Path.Combine(sourceRoot, fileName);
            if (File.Exists(source))
            {
                File.Copy(source, Path.Combine(destinationRoot, fileName), true);
            }
        }
    }
}
