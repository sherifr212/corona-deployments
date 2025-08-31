using System;
using System.Collections.Generic;
using System.IO;
using CoronaDeployments.Core.RepositoryImporter;

namespace CoronaDeployments.Core.Configuration
{
    public static class ConfigurationValidator
    {
        public static class ValidationResult
        {
            public static ValidationResult<T> Success<T>(T value) => new(value, Array.Empty<string>());
            public static ValidationResult<T> Failure<T>(params string[] errors) => new(default, errors);
        }

        public class ValidationResult<T>
        {
            public T? Value { get; }
            public string[] Errors { get; }
            public bool IsValid => Errors.Length == 0;

            internal ValidationResult(T? value, string[] errors)
            {
                Value = value;
                Errors = errors;
            }
        }

        /// <summary>
        /// Validates the application configuration for security and consistency
        /// </summary>
        public static ValidationResult<AppConfiguration> ValidateAppConfiguration(AppConfiguration config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("AppConfiguration cannot be null");
                return ValidationResult.Failure<AppConfiguration>(errors.ToArray());
            }

            if (string.IsNullOrWhiteSpace(config.BaseDirectory))
            {
                errors.Add("BaseDirectory cannot be null or empty");
            }
            else
            {
                // Validate path security
                if (IsUnsafePath(config.BaseDirectory))
                {
                    errors.Add($"BaseDirectory contains potentially unsafe path: {config.BaseDirectory}");
                }

                // Try to create directory if it doesn't exist
                try
                {
                    if (!Directory.Exists(config.BaseDirectory))
                    {
                        Directory.CreateDirectory(config.BaseDirectory);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Cannot access or create BaseDirectory '{config.BaseDirectory}': {ex.Message}");
                }
            }

            return errors.Count == 0 
                ? ValidationResult.Success(config)
                : ValidationResult.Failure<AppConfiguration>(errors.ToArray());
        }

        /// <summary>
        /// Validates repository authentication information
        /// </summary>
        public static ValidationResult<AuthInfo> ValidateAuthInfo(AuthInfo authInfo)
        {
            var errors = new List<string>();

            if (authInfo == null)
            {
                errors.Add("Authentication information cannot be null");
                return ValidationResult.Failure<AuthInfo>(errors.ToArray());
            }

            // Check for potential security issues
            if (!string.IsNullOrEmpty(authInfo.Username) && authInfo.Username.Contains("@"))
            {
                // This might be an email - ensure it's not accidentally a sensitive value
                if (authInfo.Username.ToLowerInvariant().Contains("password") || 
                    authInfo.Username.ToLowerInvariant().Contains("secret"))
                {
                    errors.Add("Username appears to contain sensitive information");
                }
            }

            if (!string.IsNullOrEmpty(authInfo.Password))
            {
                // Basic password validation
                if (authInfo.Password.Length < 3)
                {
                    errors.Add("Password appears to be too short or a placeholder");
                }

                // Check if password is obviously a placeholder
                var lowerPassword = authInfo.Password.ToLowerInvariant();
                if (lowerPassword == "password" || lowerPassword == "test" || 
                    lowerPassword == "placeholder" || lowerPassword == "change-me")
                {
                    errors.Add("Password appears to be a placeholder value");
                }
            }

            return errors.Count == 0 
                ? ValidationResult.Success(authInfo)
                : ValidationResult.Failure<AuthInfo>(errors.ToArray());
        }

        /// <summary>
        /// Validates connection string for basic security
        /// </summary>
        public static ValidationResult<string> ValidateConnectionString(string connectionString, string name)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add($"{name} connection string cannot be null or empty");
                return ValidationResult.Failure<string>(errors.ToArray());
            }

            // Check for obvious security issues
            var lowerCs = connectionString.ToLowerInvariant();
            
            if (lowerCs.Contains("password=") && !lowerCs.Contains("integrated security=true"))
            {
                // Check if password is in plain text and looks suspicious
                if (lowerCs.Contains("password=password") || 
                    lowerCs.Contains("password=admin") ||
                    lowerCs.Contains("password=test"))
                {
                    errors.Add($"{name} connection string contains a suspicious password");
                }
            }

            // Check for localhost in production (if we can determine environment)
            if (lowerCs.Contains("localhost") || lowerCs.Contains("127.0.0.1"))
            {
                // This is just a warning - might be intentional for development
                errors.Add($"Warning: {name} connection string points to localhost");
            }

            return errors.Count == 0 
                ? ValidationResult.Success(connectionString)
                : ValidationResult.Failure<string>(errors.ToArray());
        }

        private static bool IsUnsafePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            // Check for directory traversal patterns
            if (path.Contains("..") || path.Contains("~"))
                return true;

            // Check for system directories (basic check)
            var lowerPath = path.ToLowerInvariant();
            if (lowerPath.StartsWith("/root") || 
                lowerPath.StartsWith("/etc") || 
                lowerPath.StartsWith("/bin") ||
                lowerPath.StartsWith("/sbin") ||
                lowerPath.StartsWith("c:\\windows") ||
                lowerPath.StartsWith("c:\\program files"))
            {
                return true;
            }

            return false;
        }
    }
}