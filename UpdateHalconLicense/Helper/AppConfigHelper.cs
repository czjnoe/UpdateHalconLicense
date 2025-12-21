using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using UpdateHalconLicense.Models;

namespace UpdateHalconLicense.Helper
{
    public static class AppConfigHelper
    {
        private static readonly string _appSettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json");

        public static Appsetting Appsetting { get; private set; }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        static AppConfigHelper()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private static void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    Appsetting = default;
                    return;
                }

                var jsonContent = File.ReadAllText(_appSettingsPath);
                Appsetting = JsonSerializer.Deserialize<Appsetting>(jsonContent, _jsonOptions);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 根据键路径获取配置值
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="keyPath">键路径，使用冒号分隔，例如 "Section:SubSection:Key"</param>
        /// <returns>配置值</returns>
        public static T GetSetting<T>(string keyPath)
        {
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                throw new ArgumentException("键路径不能为空", nameof(keyPath));
            }

            try
            {
                if (!File.Exists(_appSettingsPath))
                {
                    return default;
                }

                var jsonContent = File.ReadAllText(_appSettingsPath);
                var jsonNode = JsonNode.Parse(jsonContent);

                var pathSegments = keyPath.Split(':');
                var currentNode = jsonNode;

                // 遍历路径查找目标节点
                foreach (var segment in pathSegments)
                {
                    if (currentNode == null)
                    {
                        return default;
                    }

                    currentNode = currentNode[segment];
                }

                if (currentNode == null)
                {
                    return default;
                }

                // 反序列化为目标类型
                return JsonSerializer.Deserialize<T>(currentNode.ToJsonString(), _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新指定键路径的配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="keyPath">键路径，使用冒号分隔，例如 "Section:SubSection:Key"</param>
        /// <param name="value">新值</param>
        public static void UpdateSetting<T>(string keyPath, T value)
        {
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                throw new ArgumentException("键路径不能为空", nameof(keyPath));
            }

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

                    // 如果节点不存在，创建新的 JsonObject
                    if (currentNode[segment] == null)
                    {
                        currentNode[segment] = new JsonObject();
                    }

                    currentNode = currentNode[segment];
                }

                // 获取最后一个键名并设置值
                var finalKey = pathSegments[pathSegments.Length - 1];
                currentNode[finalKey] = JsonValue.Create(value);

                // 保存修改后的 JSON
                File.WriteAllText(_appSettingsPath, jsonNode.ToJsonString(_jsonOptions));

                // 重新加载配置
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"更新配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存整个配置对象
        /// </summary>
        /// <typeparam name="T">配置对象类型</typeparam>
        /// <param name="config">配置对象</param>
        public static void SaveSetting<T>(T config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_appSettingsPath, json);

                // 重新加载配置
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void Reload()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// 检查配置文件是否存在
        /// </summary>
        public static bool ConfigFileExists()
        {
            return File.Exists(_appSettingsPath);
        }
    }
}
