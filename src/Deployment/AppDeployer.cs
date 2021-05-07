namespace iPhoneController.Deployment
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using iPhoneController.Diagnostics;
    using iPhoneController.Extensions;
    using iPhoneController.Models;
    using iPhoneController.Utils;

    public class AppDeployer
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger("IPA-DEPLOY");
        private readonly string _developer;
        private readonly string _provisioningProfile;

        #endregion

        #region Properties

        public string SignedReleaseFileName { get; private set; }

        public bool ResignApp { get; set; }

        #endregion

        #region Events

        internal event EventHandler<DeployEventArgs> DeployCompleted;
        private void OnDeployCompleted(Device device, bool success)
        {
            DeployCompleted?.Invoke(this, new DeployEventArgs(device, success));
        }

        #endregion

        #region Constructor

        public AppDeployer(string developer, string provisioningProfile)
        {
            if (string.IsNullOrEmpty(developer))
            {
                throw new ArgumentNullException(developer, "Developer cannot be empty");
            }
            _developer = developer;

            if (string.IsNullOrEmpty(provisioningProfile))
            {
                throw new ArgumentNullException(provisioningProfile, "provisioning profile cannot be empty");
            }
            _provisioningProfile = provisioningProfile;
            ResignApp = true;
        }

        #endregion

        #region Public Methods

        public bool Resign(string megaLink, string version)
        {
            var fileName = $"J{version}.ipa";
            var releaseName = Path.Combine(Strings.ReleasesFolder, fileName);
            var releaseNameSigned = Path.Combine(
                Strings.ReleasesFolder,
                Path.GetFileNameWithoutExtension(fileName) + "Signed.ipa"
            );
            SignedReleaseFileName = releaseNameSigned;
            if (File.Exists(releaseName))
            {
                // Already exists
                _logger.Info($"Latest ipa already downloaded, skipping...");
            }
            else
            {
                // Download new version
                _logger.Info($"Downloading IPA from {megaLink} to {releaseName}");
                if (!DownloadFile(megaLink, releaseName))
                {
                    _logger.Warn($"Failed to download IPA from {megaLink}, is megatools installed?");
                    return false;
                }
            }
            if (File.Exists(releaseNameSigned))
            {
                _logger.Info($"Signed IPA of latest already exists!");
                // Already exists
                return true;
            }

            var result = InternalResignApp(releaseName, releaseNameSigned);
            if (!result)
            {
                _logger.Error($"Unknown error occurred while resigning ipa file {releaseName}");
            }
            //Deploy(releaseNameSigned, Strings.All);
            return result;
        }

        public void Deploy(string appPath, string deviceNames = Strings.All)
        {
            var successful = new List<string>();
            var failed = new List<string>();
            var devices = Device.GetAll();
            var deployAppDevices = new List<string>(deviceNames.RemoveSpaces());
            if (string.Compare(deviceNames, Strings.All, true) == 0)
            {
                deployAppDevices = devices.Keys.ToList();
            }
            _logger.Info($"Deploying app {appPath} to {string.Join(", ", deployAppDevices)}");
            Parallel.ForEach(deployAppDevices, deviceName =>
            {
                if (!devices.ContainsKey(deviceName))
                {
                    _logger.Warn($"{deviceName} does not exist in device list, skipping deploy pogo.");
                }
                else
                {
                    var device = devices[deviceName];
                    var args = $"--id {device.Uuid} --bundle {appPath}";
                    _logger.Info($"Deploying to device {device.Name} ({device.Uuid})...");
                    var output = Shell.Execute("ios-deploy", args, out var exitCode, true);
                    var success = output.ToLower().Contains($"[100%] installed package {appPath}") ||
                                  output.ToLower().Contains("100%");
                    OnDeployCompleted(device, success);
                }
            });
        }

        public static string GetLatestAppPath()
        {
            var files = Directory.GetFiles(Strings.ReleasesFolder, "*.ipa", SearchOption.TopDirectoryOnly)
                                 .Where(x => x.ToLower().Contains("signed"))
                                 .ToList();
            // Sort descending
            files.Sort((a, b) => b.CompareTo(a));
            return files.FirstOrDefault();
        }

        #endregion

        #region Private Methods

        private bool InternalResignApp(string ipaPath, string ipaPathSigned)
        {
            _logger.Info($"Beginning to resign {ipaPath}");
            var outDir = Path.Combine(Path.GetTempPath(), "app");
            var payloadDir = Path.Combine(outDir, "Payload");
            var pogoDir = Path.Combine(payloadDir, "pokemongo.app");
            var pogoInfoPlist = Path.Combine(pogoDir, "Info.plist");

            // Create temp directory we'll be doing everything in
            CreateTempDirectory(outDir);

            // Unzip ipa file
            ZipFile.ExtractToDirectory(ipaPath, outDir, true);

            // Delete __MAXOSX folder
            var macosxFolder = Path.Combine(outDir, "__MAXOSX");
            DeleteMacOsxFolder(macosxFolder);

            // Remove NSAllowsArbitraryLoadsInWebContent
            if (!RemoveNSAllowsArbitraryLoadsInWebContent(pogoInfoPlist))
            {
                _logger.Warn($"Failed to remove NSAllowsArbitraryLoadsInWebContent plist entry from Info.plist");
            }

            // Copy provisioning profile to payload folder
            _logger.Info($"Copying provisioning profile to payload folder");
            var provisioningProfilePath = Path.Combine(Strings.ProfilesFolder, _provisioningProfile);
            File.Copy(provisioningProfilePath, $"{pogoDir}/embedded.mobileprovision", true);

            // Extra provisioning profile entitlements
            _logger.Info($"Signing mobile provisioning profile...");
            var provisioningPath = Path.Combine(Path.GetDirectoryName(outDir), "provisioning.plist");
            var provisioningData = Shell.Execute("/usr/bin/security", $"cms -D -i {pogoDir}/embedded.mobileprovision", out var _);
            File.WriteAllText(provisioningPath, provisioningData);

            _logger.Info($"Extracting entitlements from mobileprovisioning profile...");
            var entitlementsPath = Path.Combine(Path.GetDirectoryName(outDir), "entitlements.plist");
            var entitlementsData = Shell.Execute(Strings.PlistBuddyPath, $@"-x -c ""Print:Entitlements"" {provisioningPath}", out var _);
            File.WriteAllText(entitlementsPath, entitlementsData);

            // Get list of compenents for resigning
            _logger.Info($"Getting list of compenents for resigning with {_developer}");
            SignComponents(outDir, entitlementsPath);

            // Copy custom config
            var configPath = Path.Combine(Strings.ReleasesFolder, "config/config.json");
            var destinationConfigPath = Path.Combine(pogoDir, "config.json");
            CopyConfig(configPath, destinationConfigPath);

            // Sign frameworks and dynamic libraries
            var frameworksPath = Path.Combine(pogoDir, "Frameworks");
            SignFrameworks(frameworksPath);

            // Zip payload folder
            _logger.Info("Zipping payload folder...");
            var signedPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".zip");
            ZipFile.CreateFromDirectory(outDir, signedPath);

            // Move signed IPA to releases folder
            _logger.Info($"Moving signed IPA file to releases folder...");
            File.Move(signedPath, ipaPathSigned);

            // Cleanup and remove temp folder
            _logger.Info($"Deleting temp folder {outDir}...");
            Directory.Delete(outDir, true);
            File.Delete(provisioningPath);
            File.Delete(entitlementsPath);
            _logger.Info($"Your ipa has been signed into {ipaPathSigned}");
            return true;
        }

        private void Codesign(string file, bool isContinued = false, string entitlementsPath = null)
        {
            var sb = new StringBuilder();
            if (isContinued) sb.Append("--continue ");
            sb.Append("-f -s ");
            sb.Append($@"""{_developer}"" ");
            if (!string.IsNullOrEmpty(entitlementsPath))
                sb.Append($"--entitlements {entitlementsPath} ");
            sb.Append(file);

            var result = Shell.Execute(Strings.CodesignPath, sb.ToString(), out var _);
            //_logger.Debug($"Codesign result for {file}: {result}");
        }

        private void DeleteMacOsxFolder(string macosxFolder)
        {
            if (Directory.Exists(macosxFolder))
            {
                Directory.Delete(macosxFolder, true);
            }
        }

        private void SignComponents(string appPath, string entitlementsPath)
        {
            var files = GetComponentFiles(appPath);
            foreach (var file in files)
            {
                _logger.Debug($"Signing component {file}...");
                Codesign(file, true, entitlementsPath);
            }
        }

        private void CopyConfig(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                _logger.Info($"Copying custom config to payload folder.");
                File.Copy(sourcePath, destinationPath);
            }
            else
            {
                _logger.Warn($"No custom config.json file found at {sourcePath}");
            }
        }

        private void SignFrameworks(string frameworksPath)
        {
            var frameworkFiles = Directory.GetFiles(frameworksPath);
            _logger.Info($"Signing frameworks...");
            foreach (var frameworkFile in frameworkFiles)
            {
                _logger.Debug($"Signing framework {frameworkFile}");
                Codesign(frameworkFile);
            }
        }

        private bool DownloadFile(string megaLink, string destinationPath)
        {
            var result = Shell.Execute("megadl", $"{megaLink} --path={destinationPath}", out var megadlExitCode);
            return megadlExitCode == 0 || (result?.ToLower().Contains("downloaded") ?? false);
        }

        private static bool RemoveNSAllowsArbitraryLoadsInWebContent(string infoPlist)
        {
            try
            {
                var allowsArbitraryLoadsResult = Shell.Execute(Strings.PlistBuddyPath, $@"-c ""Delete :NSAppTransportSecurity"" {infoPlist}", out var _);
                _logger.Debug($"Remove 'AllowsArbitraryLoadsInWebContent result: {allowsArbitraryLoadsResult}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return false;
            }
        }

        private static List<string> GetComponentFiles(string path)
        {
            var frameworkFiles = Directory.GetDirectories(path, "*.framework", SearchOption.AllDirectories);
            var dylibFiles = Directory.GetFiles(path, "*.dylib", SearchOption.AllDirectories);
            var appFiles = Directory.GetDirectories(path, "*.app", SearchOption.AllDirectories);
            var appexFiles = Directory.GetDirectories(path, "*.appex", SearchOption.AllDirectories);
            var list = new List<string>();
            list.AddRange(frameworkFiles);
            list.AddRange(dylibFiles);
            list.AddRange(appFiles);
            list.AddRange(appexFiles);
            return list;
        }

        private void CreateTempDirectory(string tempDir)
        {
            if (!Directory.Exists(tempDir))
            {
                _logger.Info($"Creating temp build directory: {tempDir}");
                Directory.CreateDirectory(tempDir);
            }
        }

        #endregion
    }

    internal class DeployEventArgs : EventArgs
    {
        public Device Device { get; set; }

        public bool Success { get; set; }

        internal DeployEventArgs(Device device, bool success)
        {
            Device = device;
            Success = success;
        }
    }
}