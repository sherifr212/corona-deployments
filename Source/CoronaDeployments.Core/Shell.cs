using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoronaDeployments.Core
{
    public static class Shell
    {
        // Whitelist of allowed command executables for security
        private static readonly HashSet<string> AllowedExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "git", "git.exe",
            "svn", "svn.exe", 
            "dotnet", "dotnet.exe",
            "msbuild", "msbuild.exe",
            "nuget", "nuget.exe"
        };

        /// <summary>
        /// Executes a shell command with input validation and security measures
        /// </summary>
        /// <param name="executable">The executable to run (must be in whitelist)</param>
        /// <param name="arguments">Arguments to pass to the executable</param>
        /// <param name="workingDirectory">Working directory for the process</param>
        /// <returns>The output from the command execution</returns>
        public static async Task<string> Execute(string executable, string arguments = "", string workingDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(executable))
                throw new ArgumentException("Executable cannot be null or empty", nameof(executable));

            // Validate executable is in whitelist for security
            var executableName = Path.GetFileName(executable);
            if (!AllowedExecutables.Contains(executableName))
                throw new SecurityException($"Executable '{executableName}' is not in the allowed list for security reasons");

            // Additional validation to prevent command injection
            ValidateInput(executable);
            if (!string.IsNullOrEmpty(arguments))
                ValidateInput(arguments);

            return await Task.Run(() =>
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = executable,
                            Arguments = arguments ?? string.Empty,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false, // Important for security
                            CreateNoWindow = true,
                            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
                        };

                        process.Start();

                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                        {
                            throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {error}");
                        }

                        return output;
                    }
                }
                catch (Exception ex) when (!(ex is SecurityException))
                {
                    throw new InvalidOperationException($"Failed to execute command '{executable} {arguments}': {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Legacy method for backward compatibility - DEPRECATED
        /// </summary>
        [Obsolete("Use Execute(executable, arguments) instead for better security")]
        public static async Task<string> Execute(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentException("Command cannot be null or empty", nameof(cmd));

            // Try to parse the command safely
            var parts = ParseCommand(cmd);
            if (parts.Length == 0)
                throw new ArgumentException("Invalid command format", nameof(cmd));

            var executable = parts[0];
            var arguments = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : string.Empty;

            return await Execute(executable, arguments);
        }

        private static void ValidateInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;

            // Check for dangerous characters that could be used for command injection
            var dangerousPatterns = new[]
            {
                @"[;&|<>]",      // Command separators and redirections
                @"[`$]",         // Command substitution
                @"\.\.",         // Directory traversal
                @"[\r\n]"        // Line breaks
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(input, pattern))
                {
                    throw new SecurityException($"Input contains potentially dangerous characters: {input}");
                }
            }
        }

        private static string[] ParseCommand(string cmd)
        {
            // Simple command parsing - in production, consider using a more robust parser
            return cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}
