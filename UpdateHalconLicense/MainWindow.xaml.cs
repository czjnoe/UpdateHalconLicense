using Ookii.Dialogs.Wpf;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using UpdateHalconLicense.Helper;
using UpdateHalconLicense.Models;

namespace UpdateHalconLicense
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> proxyList = new List<string>();
        private readonly HttpClient httpClient;
        private DispatcherTimer autoUpdateTimer;
        private bool useProxy = true; // 是否使用代理
        private WindowsServiceManager _serviceManager;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 HttpClient - 增加超时时间和重试机制
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                MaxConnectionsPerServer = 10
            };

            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "HalconLicenseUpdater");
            httpClient.Timeout = TimeSpan.FromMinutes(5); // 增加超时到5分钟

            // 加载配置
            LoadConfig();

            // 初始化自动更新
            InitializeAutoUpdate();

            // 记录启动日志
            LogMessage("程序启动成功");
            LogMessage($"GitHub 仓库: lovelyyoshino/Halcon_licenses");
            LogMessage($"可用代理节点: {proxyList.Count} 个");

            LoadRegisterServiceState();
        }

        #region 配置管理

        private void LoadConfig()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AppConfigHelper.Appsetting.HalconPath))
                {
                    string halconRootValue = Environment.GetEnvironmentVariable("HALCONROOT", EnvironmentVariableTarget.Machine);
                    if (string.IsNullOrWhiteSpace(halconRootValue))
                        halconRootValue = Environment.GetEnvironmentVariable("HALCONROOT", EnvironmentVariableTarget.User);
                    string licensePath = string.IsNullOrWhiteSpace(halconRootValue) ? "" : Path.Combine(halconRootValue, "license");
                    if (Directory.Exists(licensePath))
                        AppConfigHelper.Appsetting.HalconPath = licensePath;
                }

                txtHalconPath.Text = AppConfigHelper.Appsetting.HalconPath ?? "";
                txtDownloadPath.Text = AppConfigHelper.Appsetting.DownloadPath;
                chkAutoUpdate.IsChecked = AppConfigHelper.Appsetting.AutoUpdateEnabled;
                chkUseProxy.IsChecked = AppConfigHelper.Appsetting.UseProxy;
                cmbUpdateInterval.SelectedIndex = AppConfigHelper.Appsetting.UpdateIntervalIndex;
                proxyList = AppConfigHelper.Appsetting.Proxys;
                LogMessage("配置加载成功");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 加载配置失败: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new Appsetting
                {
                    HalconPath = txtHalconPath.Text,
                    DownloadPath = txtDownloadPath.Text,
                    AutoUpdateEnabled = chkAutoUpdate.IsChecked == true,
                    UseProxy = chkUseProxy.IsChecked == true,
                    UpdateIntervalIndex = cmbUpdateInterval.SelectedIndex,
                    Proxys = proxyList,
                };
                AppConfigHelper.SaveSetting(config);

                LogMessage("✓ 配置已保存");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 保存配置失败: {ex.Message}");
            }
        }

        #endregion

        #region 自动更新

        private void InitializeAutoUpdate()
        {
            autoUpdateTimer = new DispatcherTimer();
            autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
            UpdateTimerInterval();

            // 如果启用了自动更新，则启动定时器
            if (chkAutoUpdate.IsChecked == true)
            {
                autoUpdateTimer.Start();
                LogMessage("自动更新定时器已启动");
            }
        }

        private void UpdateTimerInterval()
        {
            var intervals = new[] {
                TimeSpan.FromHours(1),      // 每小时
                TimeSpan.FromHours(6),      // 每6小时
                TimeSpan.FromDays(1),       // 每天
                TimeSpan.FromDays(7)        // 每周
            };

            int index = cmbUpdateInterval.SelectedIndex;
            if (index >= 0 && index < intervals.Length)
            {
                autoUpdateTimer.Interval = intervals[index];
            }
        }

        private void ChkAutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTimerInterval();
            autoUpdateTimer.Start();
            SaveConfig();
            LogMessage("✓ 自动更新已启用");
        }

        private void ChkAutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            autoUpdateTimer.Stop();
            SaveConfig();
            LogMessage("⊗ 自动更新已禁用");
        }

        private async void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            LogMessage("⏰ 执行定时自动更新检查...");
            await CheckAndDownloadLicense(true);
        }

        #endregion

        #region UI事件处理

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "选择 Halcon 安装目录",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrEmpty(txtHalconPath.Text) && Directory.Exists(txtHalconPath.Text))
            {
                dialog.SelectedPath = txtHalconPath.Text;
            }

            if (dialog.ShowDialog() == true)
            {
                txtHalconPath.Text = dialog.SelectedPath;
                SaveConfig();
                LogMessage($"✓ Halcon 路径已设置: {dialog.SelectedPath}");
            }
        }

        private void BtnBrowseDownload_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "选择 License 下载保存目录",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(txtDownloadPath.Text) && Directory.Exists(txtDownloadPath.Text))
            {
                dialog.SelectedPath = txtDownloadPath.Text;
            }

            if (dialog.ShowDialog() == true)
            {
                txtDownloadPath.Text = dialog.SelectedPath;
                SaveConfig();
                LogMessage($"✓ 下载路径已设置: {dialog.SelectedPath}");
            }
        }

        private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            await CheckAndDownloadLicense(false);
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            await DownloadCurrentMonthLicense();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            await InstallLicense();
        }

        #endregion

        #region 核心功能

        private async Task CheckAndDownloadLicense(bool isAutoUpdate)
        {
            try
            {
                SetStatus("正在检查最新 License...");
                LogMessage("═══════════════════════════════");
                LogMessage($"开始检查 License 更新 ({(isAutoUpdate ? "自动" : "手动")})");

                var currentMonth = GetCurrentMonthFolder();
                LogMessage($"当前月份文件夹: {currentMonth}");

                var licenseUrls = await GetAllLicenseUrls(currentMonth);

                if (licenseUrls == null || licenseUrls.Count == 0)
                {
                    LogMessage($"❌ 未找到 {currentMonth} 的 License 文件");
                    SetStatus("未找到最新 License");

                    if (!isAutoUpdate)
                    {
                        MessageBox.Show($"未找到 {currentMonth} 的 License 文件\n请稍后再试或联系维护者",
                            "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                LogMessage($"找到 {licenseUrls.Count} 个 License 文件");

                // 下载所有 License 文件
                int successCount = 0;
                foreach (var fileInfo in licenseUrls)
                {
                    try
                    {
                        await DownloadSingleLicense(fileInfo.Url, fileInfo.FileName, currentMonth);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"❌ 下载 {fileInfo.FileName} 失败: {ex.Message}");
                    }
                }

                LogMessage($"✓ 下载完成! 成功下载 {successCount}/{licenseUrls.Count} 个文件");

                // 如果启用了自动更新且设置了 Halcon 路径，则自动安装
                if (isAutoUpdate && !string.IsNullOrWhiteSpace(txtHalconPath.Text))
                {
                    await InstallLicense(true);
                }
                else if (!isAutoUpdate && successCount > 0)
                {
                    var result = MessageBox.Show(
                        $"成功下载 {successCount} 个 License 文件！\n\n是否立即安装到 Halcon？",
                        "下载完成",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await InstallLicense(false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 检查更新失败: {ex.Message}");
                SetStatus("检查更新失败");

                if (!isAutoUpdate)
                {
                    MessageBox.Show($"检查更新失败:\n{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DownloadCurrentMonthLicense()
        {
            try
            {
                SetStatus("正在下载当月所有 License...");
                LogMessage("═══════════════════════════════");
                LogMessage("开始下载当月所有 License 文件");

                var currentMonth = GetCurrentMonthFolder();
                var licenseUrls = await GetAllLicenseUrls(currentMonth);

                if (licenseUrls == null || licenseUrls.Count == 0)
                {
                    LogMessage($"❌ 未找到 {currentMonth} 的 License 文件");
                    MessageBox.Show($"未找到 {currentMonth} 的 License 文件", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                LogMessage($"找到 {licenseUrls.Count} 个 License 文件");

                int successCount = 0;
                int failCount = 0;

                foreach (var fileInfo in licenseUrls)
                {
                    try
                    {
                        await DownloadSingleLicense(fileInfo.Url, fileInfo.FileName, currentMonth);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"❌ 下载 {fileInfo.FileName} 失败: {ex.Message}");
                        failCount++;
                    }
                }

                LogMessage("═══════════════════════════════");
                LogMessage($"✓ 下载完成! 成功: {successCount}, 失败: {failCount}");

                MessageBox.Show(
                    $"License 下载完成！\n\n" +
                    $"成功: {successCount} 个\n" +
                    $"失败: {failCount} 个\n\n" +
                    $"文件保存在:\n{txtDownloadPath.Text}",
                    "下载完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 下载失败: {ex.Message}");
                MessageBox.Show($"下载失败:\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<List<LicenseFileInfo>> GetAllLicenseUrls(string monthFolder)
        {
            try
            {
                LogMessage($"正在获取 {monthFolder} 目录内容...");
                var apiUrl = $"{AppConfigHelper.Appsetting.HalconDownloadUrl}{monthFolder}";

                var response = await httpClient.GetStringAsync(apiUrl);
                var files = JsonSerializer.Deserialize<List<GitHubFile>>(response);

                if (files == null || files.Count == 0)
                {
                    LogMessage("目录为空或无法访问");
                    return new List<LicenseFileInfo>();
                }

                // 查找所有 .dat 文件
                var licenseFiles = files
                    .Where(f => f.name != null && f.name.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                    .Select(f => new LicenseFileInfo
                    {
                        FileName = f.name,
                        Url = f.download_url,
                        Size = f.size
                    })
                    .ToList();

                if (licenseFiles.Count > 0)
                {
                    LogMessage($"✓ 找到 {licenseFiles.Count} 个 License 文件:");
                    foreach (var file in licenseFiles)
                    {
                        LogMessage($"  - {file.FileName} ({FormatFileSize(file.Size)})");
                    }
                }
                else
                {
                    LogMessage("未找到 .dat 格式的 License 文件");
                }

                return licenseFiles;
            }
            catch (HttpRequestException ex)
            {
                LogMessage($"❌ HTTP 请求失败: {ex.Message}");
                return new List<LicenseFileInfo>();
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 获取 License URL 失败: {ex.Message}");
                return new List<LicenseFileInfo>();
            }
        }

        private async Task DownloadSingleLicense(string url, string fileName, string monthFolder)
        {
            var downloadPath = txtDownloadPath.Text;
            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                throw new Exception("请设置下载保存目录");
            }

            // 创建月份子目录
            var monthPath = Path.Combine(downloadPath, monthFolder);
            if (!Directory.Exists(monthPath))
            {
                Directory.CreateDirectory(monthPath);
                LogMessage($"✓ 创建目录: {monthPath}");
            }

            var filePath = Path.Combine(monthPath, fileName);

            // 遍历所有代理尝试下载
            bool useProxy = chkUseProxy.IsChecked == true;
            var proxiesToTry = useProxy ? proxyList : new List<string> { "" }; // 如果不使用代理，只尝试直连

            foreach (var proxy in proxiesToTry)
            {
                try
                {
                    var downloadUrl = string.IsNullOrEmpty(proxy) ? url : proxy + url;
                    var proxyName = string.IsNullOrEmpty(proxy) ? "直连" : proxy.Replace("https://", "").TrimEnd('/');

                    LogMessage($"开始下载: {fileName}");
                    LogMessage($"  使用节点: {proxyName}");

                    var content = await DownloadWithProgress(downloadUrl, fileName);
                    await File.WriteAllBytesAsync(filePath, content);

                    LogMessage($"✓ 下载完成: {fileName} ({FormatFileSize(content.Length)})");
                    return; // 成功下载，退出
                }
                catch (Exception ex)
                {
                    var proxyName = string.IsNullOrEmpty(proxy) ? "直连" : proxy;
                    LogMessage($"  ✗ 节点失败: {proxyName.Replace("https://", "")} - {ex.Message}");

                    // 如果不是最后一个代理，继续尝试下一个
                    if (proxy != proxiesToTry.Last())
                    {
                        LogMessage($"  → 切换到下一个节点...");
                        await Task.Delay(1000); // 短暂延迟
                        continue;
                    }
                }
            }

            // 所有代理都失败
            throw new Exception($"所有下载节点均失败，无法下载 {fileName}");
        }

        private async Task<byte[]> DownloadWithProgress(string url, string fileName)
        {
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var memoryStream = new MemoryStream())
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;
                    var lastProgress = 0;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await memoryStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            if (progress >= lastProgress + 20) // 每20%显示一次进度
                            {
                                LogMessage($"  下载进度: {progress}% ({FormatFileSize(totalRead)}/{FormatFileSize(totalBytes)})");
                                lastProgress = progress;
                            }
                        }
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        private async Task InstallLicense(bool isSilent = false)
        {
            try
            {
                var halconPath = txtHalconPath.Text;
                if (string.IsNullOrWhiteSpace(halconPath))
                {
                    if (!isSilent)
                    {
                        MessageBox.Show("请先设置 Halcon 安装目录", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    LogMessage("❌ 未设置 Halcon 安装目录");
                    return;
                }

                if (!Directory.Exists(halconPath))
                {
                    if (!isSilent)
                    {
                        MessageBox.Show("Halcon 安装目录不存在", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    LogMessage($"❌ Halcon 目录不存在: {halconPath}");
                    return;
                }

                LogMessage("═══════════════════════════════");
                LogMessage("开始安装 License");

                // 确保 license 目录存在
                var licensePath = Path.Combine(halconPath, "license");
                if (!Directory.Exists(licensePath))
                {
                    Directory.CreateDirectory(licensePath);
                    LogMessage($"✓ 创建 license 目录: {licensePath}");
                }

                // 查找当月下载的所有 license 文件
                var currentMonth = GetCurrentMonthFolder();
                var monthPath = Path.Combine(txtDownloadPath.Text, currentMonth);

                if (!Directory.Exists(monthPath))
                {
                    if (!isSilent)
                    {
                        MessageBox.Show("请先下载当月的 License 文件", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    LogMessage($"❌ 月份目录不存在: {monthPath}");
                    return;
                }

                var datFiles = Directory.GetFiles(monthPath, "*.dat");

                if (datFiles.Length == 0)
                {
                    if (!isSilent)
                    {
                        MessageBox.Show("未找到 License 文件", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    LogMessage($"❌ 未找到 .dat 文件");
                    return;
                }

                LogMessage($"找到 {datFiles.Length} 个 License 文件，开始安装...");

                int installedCount = 0;
                foreach (var sourceFile in datFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(sourceFile);
                        var destFile = Path.Combine(licensePath, fileName);

                        // 备份已存在的文件
                        if (File.Exists(destFile))
                        {
                            var backupFile = Path.Combine(licensePath,
                                $"{Path.GetFileNameWithoutExtension(fileName)}_backup_{DateTime.Now:yyyyMMddHHmmss}.dat");
                            File.Copy(destFile, backupFile, true);
                            LogMessage($"  备份: {fileName} → {Path.GetFileName(backupFile)}");
                        }

                        // 复制新 license
                        File.Copy(sourceFile, destFile, true);
                        LogMessage($"✓ 安装: {fileName}");
                        installedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"❌ 安装 {Path.GetFileName(sourceFile)} 失败: {ex.Message}");
                    }
                }

                LogMessage("═══════════════════════════════");
                LogMessage($"✓ 安装完成! 成功安装 {installedCount}/{datFiles.Length} 个文件");
                LogMessage($"  安装目录: {licensePath}");
                LogMessage($"  提示: 请重启 Halcon 使 License 生效");

                SetStatus("安装完成");

                if (!isSilent)
                {
                    MessageBox.Show(
                        $"License 安装成功！\n\n" +
                        $"成功安装: {installedCount} 个文件\n" +
                        $"安装位置: {licensePath}\n\n" +
                        "请重启 Halcon 后生效。",
                        "成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 安装失败: {ex.Message}");
                if (!isSilent)
                {
                    MessageBox.Show($"安装失败:\n{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region 辅助方法

        private string GetCurrentMonthFolder()
        {
            var now = DateTime.Now;
            return $"{now.Year}.{now.Month:D2}";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                txtLog.AppendText($"[{timestamp}] {message}\n");
                txtLog.ScrollToEnd();
            });
        }

        private void SetStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = status;
            });
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 清理资源
            autoUpdateTimer?.Stop();
            httpClient?.Dispose();

            LogMessage("程序关闭");
        }

        private void BtnOpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $@"/root, {txtDownloadPath.Text.Trim()}",
            };
            System.Diagnostics.Process.Start(startInfo);
        }

        private void BtnOpenHalconDirectory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $@"/root, {txtHalconPath.Text.Trim()}",
            };
            System.Diagnostics.Process.Start(startInfo);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            GitManagerPage page = new GitManagerPage(proxyList);
            var dialogResult = page.ShowDialog();
            if (dialogResult == true)
            {
                proxyList = page.ProxyList.Select(p => p.Proxy).ToList();
                SaveConfig();
                LogMessage("✓ 代理节点已更新");
            }
        }

        private void btnRegisterService_Click(object sender, RoutedEventArgs e)
        {
            var exePath = Environment.ProcessPath ??
                                     System.Reflection.Assembly.GetExecutingAssembly().Location;
            var state = _serviceManager.InstallService(exePath);
            btnRegisterService.IsEnabled = !state;
            btnCancleRegisterService.IsEnabled = state;
        }

        private void btnCancleRegisterService_Click(object sender, RoutedEventArgs e)
        {
            var state = _serviceManager.UninstallService();
            btnRegisterService.IsEnabled = state;
            btnCancleRegisterService.IsEnabled = !state;
        }

        private void LoadRegisterServiceState()
        {
            _serviceManager = new WindowsServiceManager("UpdateHalconLicense.Service", "", "");
            var state = _serviceManager.IsServiceInstalled();
            btnRegisterService.IsEnabled = !state;
            btnCancleRegisterService.IsEnabled = state;
        }
    }
}