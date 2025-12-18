using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UpdateHalconLicense.Models;

namespace UpdateHalconLicense.Helper
{
    public static class AppConfigHelper
    {
        private static IConfigurationRoot _configuration;
        private static readonly string _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        private static Appsetting Appsetting;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        static AppConfigHelper()
        {
            LoadConfiguration();
        }

        private static void LoadConfiguration()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(_appSettingsPath, optional: false, reloadOnChange: false)
                .Build();

            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Appsetting = default;
                }
                Appsetting = System.Text.Json.JsonSerializer.Deserialize<Appsetting>(File.ReadAllText(_appSettingsPath));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T GetSetting<T>(string key)
        {
            return _configuration.GetSection(key).Get<T>();
        }

        public static void UpdateSetting<T>(string keyPath, T value)
        {
            try
            {
                // 读取整个 JSON 文件
                var jsonContent = File.ReadAllText(_appSettingsPath);
                var jsonNode = JsonNode.Parse(jsonContent);

                // 分割键路径
                var pathSegments = keyPath.Split(':');
                var currentNode = jsonNode;

                // 遍历路径直到找到目标节点（跳过最后一个段）
                for (int i = 0; i < pathSegments.Length - 1; i++)
                {
                    var segment = pathSegments[i];
                    currentNode = currentNode[segment] ??= new JsonObject();
                }

                // 获取最后一个键名
                var finalKey = pathSegments[pathSegments.Length - 1];

                // 设置最终值
                currentNode[finalKey] = JsonValue.Create(value);

                // 保存修改后的 JSON
                File.WriteAllText(_appSettingsPath, jsonNode.ToJsonString(_jsonOptions));
            }
            catch (Exception ex)
            {
                // 实际应用中应记录错误
                throw new InvalidOperationException($"更新配置失败: {ex.Message}", ex);
            }
            LoadConfiguration();// 重新加载配置
        }
    }


}
