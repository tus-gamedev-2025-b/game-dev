using System;
using Tanks.ApiClient.Client;
using Tanks.Complete.Persistence;

namespace Tanks.Complete.Utils
{
    public static class ApiConfig
    {
        private const string DefaultBasePath = "http://localhost:3000/api";
        private const string BasePathEnvVar = "TANKS_API_BASE_PATH";

        public static string ResolveBasePath()
        {
            var env = Environment.GetEnvironmentVariable(BasePathEnvVar);
            if (!string.IsNullOrWhiteSpace(env))
            {
                return Normalize(env);
            }

            var stored = EncryptedPrefs.GetString(StorageKeys.ApiBasePath, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                return Normalize(stored);
            }

            return DefaultBasePath;
        }

        public static Configuration CreateConfiguration(string accessToken = null, string basePathOverride = null)
        {
            var config = new Configuration
            {
                BasePath = Normalize(string.IsNullOrWhiteSpace(basePathOverride) ? ResolveBasePath() : basePathOverride)
            };

            if (!string.IsNullOrEmpty(accessToken))
            {
                config.AccessToken = accessToken;
            }

            return config;
        }

        private static string Normalize(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return DefaultBasePath;
            }

            return basePath.EndsWith("/") ? basePath.TrimEnd('/') : basePath;
        }
    }
}
