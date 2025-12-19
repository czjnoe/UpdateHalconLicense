using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UpdateHalconLicense
{
    /// <summary>
    /// Window服务管理器
    /// 注册、注销、检查服务
    /// </summary>
    public class WindowsServiceManager
    {
        private readonly string _serviceName;
        private readonly string _displayName;
        private readonly string _description;

        public WindowsServiceManager(string serviceName, string displayName, string description)
        {
            _serviceName = serviceName;
            _displayName = displayName;
            _description = description;
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public bool IsServiceInstalled()
        {
            try
            {
                using var sc = new ServiceController(_serviceName);
                var status = sc.Status; // 如果服务不存在会抛出异常
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 注册Windows服务
        /// </summary>
        public bool InstallService(string executablePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("此功能仅支持Windows系统");
                return false;
            }

            if (IsServiceInstalled())
            {
                Console.WriteLine($"服务 '{_serviceName}' 已经存在");
                return false;
            }

            try
            {
                // 使用 sc create 命令创建服务
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"create \"{_serviceName}\" binPath= \"{executablePath}\" start= auto DisplayName= \"{_displayName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Console.WriteLine("无法启动sc命令");
                    return false;
                }

                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    // 设置服务描述
                    SetServiceDescription();
                    Console.WriteLine($"服务 '{_serviceName}' 注册成功");

                    // 自动启动服务
                    if (StartService())
                    {
                        Console.WriteLine($"服务 '{_serviceName}' 已启动");
                    }
                    else
                    {
                        Console.WriteLine($"服务注册成功，但启动失败。请手动运行: net start {_serviceName}");
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine($"注册失败: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册服务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 注销Windows服务
        /// </summary>
        public bool UninstallService()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("此功能仅支持Windows系统");
                return false;
            }

            if (!IsServiceInstalled())
            {
                Console.WriteLine($"服务 '{_serviceName}' 不存在");
                return false;
            }

            try
            {
                // 先停止服务
                StopService();

                // 使用 sc delete 命令删除服务
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"delete \"{_serviceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Console.WriteLine("无法启动sc命令");
                    return false;
                }

                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"服务 '{_serviceName}' 注销成功");
                    return true;
                }
                else
                {
                    Console.WriteLine($"注销失败: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注销服务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取服务状态
        /// </summary>
        public string GetServiceStatus()
        {
            if (!IsServiceInstalled())
            {
                return "未安装";
            }

            try
            {
                using var sc = new ServiceController(_serviceName);
                return sc.Status.ToString();
            }
            catch (Exception ex)
            {
                return $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        private void StopService()
        {
            try
            {
                using var sc = new ServiceController(_serviceName);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    Console.WriteLine("正在停止服务...");
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    Console.WriteLine("服务已停止");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止服务时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        private bool StartService()
        {
            try
            {
                using var sc = new ServiceController(_serviceName);
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine("正在启动服务...");
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    return true;
                }
                else if (sc.Status == ServiceControllerStatus.Running)
                {
                    Console.WriteLine("服务已经在运行中");
                    return true;
                }
                else
                {
                    Console.WriteLine($"服务当前状态: {sc.Status}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动服务时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置服务描述
        /// </summary>
        private void SetServiceDescription()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"description \"{_serviceName}\" \"{_description}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch { }
        }
    }
}
