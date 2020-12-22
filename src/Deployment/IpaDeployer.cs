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

    public class IpaDeployer
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger("IPA-DEPLOY");
        private readonly string _developer;
        private readonly string _provisioningProfile;


        public string ReleasesFolder => Path.Combine
        (
            Strings.ReleasesFolder,
            "jorg"
        );

        public string SignedReleaseFileName { get; private set; }

        public bool ResignApp { get; set; }


        public IpaDeployer(string developer, string provisioningProfile)
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


        public bool Resign(string megaLink, string version)
        {
            var fileName = $"J{version}.ipa";
            var releaseName = Path.Combine(ReleasesFolder, fileName);
            var releaseNameSigned = Path.Combine(
                ReleasesFolder,
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

            var result = ResignIpa(releaseName, releaseNameSigned);
            if (!result)
            {
                _logger.Error($"Unknown error occurred while resigning ipa file {releaseName}");
            }
            //Deploy(releaseNameSigned, Strings.All);
            return result;
        }

        public void Deploy(string ipaPath, string deviceNames = Strings.All)
        {
            var devices = Device.GetAll();
            var deployAppDevices = new List<string>(deviceNames.RemoveSpaces());
            if (string.Compare(deviceNames, Strings.All, true) == 0)
            {
                deployAppDevices = devices.Keys.ToList();
            }
            Parallel.ForEach(deployAppDevices, deviceName =>
            {
                if (!devices.ContainsKey(deviceName))
                {
                    _logger.Warn($"{deviceName} does not exist in device list, skipping deploy pogo.");
                }
                else
                {
                    var uuid = devices[deviceName];
                    var args = $"--id {uuid} --bundle {ipaPath}";
                    var output = Shell.Execute("ios-deploy", args, out var exitCode);
                    _logger.Info($"Deployed Pokemon Go to {deviceName} ({uuid})\r\nOutput: {output}");
                    // TODO: OnDeployCompleted event
                }
            });
        }


        private bool ResignIpa(string ipaPath, string ipaPathSigned)
        {
            _logger.Info($"Beginning to resign {ipaPath}");
            var outDir = Path.Combine(Path.GetTempPath(), "app");
            var payloadDir = Path.Combine(outDir, "Payload");
            var pogoDir = Path.Combine(payloadDir, "pokemongo.app");
            var pogoInfoPlist = Path.Combine(pogoDir, "Info.plist");

            // Create temp directory we'll be doing everything in
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            // Unzip ipa file
            ZipFile.ExtractToDirectory(ipaPath, outDir, true);

            // Delete __MAXOSX folder
            var macosxFolder = Path.Combine(outDir, "__MAXOSX");
            if (Directory.Exists(macosxFolder))
            {
                Directory.Delete(macosxFolder, true);
            }

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
            _logger.Info($"Extracting entitlements from mobileprovisioning profile...");
            var provisioningPath = Path.Combine(Path.GetDirectoryName(outDir), "provisioning.plist");
            var provisioningData = Shell.Execute("/usr/bin/security", $"cms -D -i {pogoDir}/embedded.mobileprovision", out var _);
            File.WriteAllText(provisioningPath, provisioningData);

            var entitlementsPath = Path.Combine(Path.GetDirectoryName(outDir), "entitlements.plist");
            var entitlementsData = Shell.Execute(Strings.PlistBuddyPath, $"-x -c 'Print:Entitlements' {provisioningPath}", out var _);
            File.WriteAllText(entitlementsPath, entitlementsData);

            // Get list of compenents for resigning
            _logger.Info($"Getting list of compenents for resigning with {_developer}");
            //Shell.Execute("/usr/bin/find", $"-d {outDir} -name *.app -o -name *.appex -o -name *.framework -o -name *.dylib", out var componentsExitCode);
            var files = GetComponentFiles(outDir);
            foreach (var file in files)
            {
                //Shell.Execute(Strings.CodeSignPath, $@"--continue -f -s ""{_developer}"" --entitlements {entitlementsPath} {file}", out var _);
                Codesign(file, true, entitlementsPath);
            }

            // Copy custom config
            var configPath = Path.Combine(ReleasesFolder, "config/config.json");
            if (File.Exists(configPath))
            {
                _logger.Info($"Copying custom config to payload folder.");
                File.Copy(configPath, Path.Combine(pogoDir, "config.json"));
            }
            else
            {
                _logger.Warn($"No custom config.json file found at {configPath}");
            }

            // Sign frameworks and dynamic libraries
            var frameworksPath = Path.Combine(pogoDir, "Frameworks");
            var frameworkFiles = Directory.GetFiles(frameworksPath);
            _logger.Info($"Signing frameworks...");
            foreach (var frameworkFile in frameworkFiles)
            {
                _logger.Debug($"Signing framework {frameworkFile}");
                //Shell.Execute(Strings.CodeSignPath, $@"-f -s ""{_developer}"" {frameworkFile}", out var _);
                Codesign(frameworkFile, false);
            }

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
            _logger.Info($"Your ipa has been signed into {ipaPathSigned}");
            return true;
        }

        private void Codesign(string file, bool isContinued = false, string entitlementsPath = null)
        {
            var sb = new StringBuilder();
            if (isContinued) sb.Append("--continue ");
            sb.Append("-f -s");
            sb.Append($@"""{_developer}""");
            if (!string.IsNullOrEmpty(entitlementsPath))
                sb.Append($"--entitlements {entitlementsPath}");
            sb.Append(file);

            var result = Shell.Execute(Strings.CodesignPath, sb.ToString(), out var _);
            _logger.Debug($"Codesign result for {file}: {result}");
        }

        private bool DownloadFile(string megaLink, string destinationPath)
        {
            var result = Shell.Execute("megadl", $"{megaLink} --path={destinationPath}", out var _);
            return result?.ToLower().Contains("downloaded") ?? false;
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
            var frameworkFiles = Directory.GetFiles(path, "*.framework", SearchOption.AllDirectories);
            var dylibFiles = Directory.GetFiles(path, "*.dylib", SearchOption.AllDirectories);
            var appFiles = Directory.GetFiles(path, "*.app", SearchOption.AllDirectories);
            var appexFiles = Directory.GetFiles(path, "*.appex", SearchOption.AllDirectories);
            var list = new List<string>();
            list.AddRange(frameworkFiles);
            list.AddRange(dylibFiles);
            list.AddRange(appFiles);
            list.AddRange(appexFiles);
            return list;
        }
    }
}