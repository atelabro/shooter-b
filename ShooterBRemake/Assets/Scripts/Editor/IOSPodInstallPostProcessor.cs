using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ShooterB
{
    public static class IOSPodInstallPostProcessor
    {
        private const int PostProcessOrder = 60;
        private const int PodInstallTimeoutMilliseconds = 10 * 60 * 1000;

        [PostProcessBuild(PostProcessOrder)]
        public static void RunPodInstall(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
                return;

            string podfilePath = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfilePath))
            {
                Debug.Log("[iOSPodInstall] No Podfile found. Skipping pod install.");
                return;
            }

            ProcessResult result = RunProcess("/bin/bash", "-lc \"pod install\"", buildPath);
            if (result.ExitCode == 0)
            {
                Debug.Log($"[iOSPodInstall] pod install completed.\n{result.Output}");
                return;
            }

            string message =
                $"[iOSPodInstall] pod install failed with exit code {result.ExitCode}.\n" +
                $"Output:\n{result.Output}\nErrors:\n{result.Error}";
            throw new BuildFailedException(message);
        }

        private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
        {
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (Process process = new Process())
            {
                ProcessStartInfo startInfo = process.StartInfo;
                startInfo.FileName = fileName;
                startInfo.Arguments = arguments;
                startInfo.WorkingDirectory = workingDirectory;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                EnsurePathContainsHomebrew(startInfo);
                startInfo.EnvironmentVariables["LANG"] = "en_US.UTF-8";
                startInfo.EnvironmentVariables["LC_ALL"] = "en_US.UTF-8";

                process.OutputDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        output.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        error.AppendLine(args.Data);
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(PodInstallTimeoutMilliseconds))
                    {
                        process.Kill();
                        throw new BuildFailedException("[iOSPodInstall] pod install timed out after 10 minutes.");
                    }

                    process.WaitForExit();
                }
                catch (BuildFailedException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new BuildFailedException($"[iOSPodInstall] Failed to start pod install: {exception.Message}");
                }

                return new ProcessResult(process.ExitCode, output.ToString(), error.ToString());
            }
        }

        private static void EnsurePathContainsHomebrew(ProcessStartInfo startInfo)
        {
            const string prefix = "/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";
            string path = startInfo.EnvironmentVariables["PATH"];
            startInfo.EnvironmentVariables["PATH"] = string.IsNullOrEmpty(path)
                ? prefix
                : $"{prefix}:{path}";
        }

        private struct ProcessResult
        {
            public ProcessResult(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }

            public int ExitCode { get; }
            public string Output { get; }
            public string Error { get; }
        }
    }
}
