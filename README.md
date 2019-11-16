# iPhoneController  

## Installation  

### Prerequisites:  
- idevicediagnostics  
1. `brew update`  
2. `brew uninstall --ignore-dependencies libimobiledevice`  
3. `brew uninstall --ignore-dependencies usbmuxd`  
4. `brew install --HEAD usbmuxd`  
5. `brew unlink usbmuxd`  
6. `brew link usbmuxd`  
7. `brew install --HEAD libimobiledevice`  
8. `brew unlink libimobiledevice && brew link libimobiledevice`  
9. `brew install --HEAD ideviceinstaller`  
10. `brew unlink ideviceinstaller && brew link ideviceinstaller`  
11. `sudo chmod -R 777 /var/db/lockdown/`  

- iPhoneController
1. `wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh && rm dotnet-install.sh`  
2. `git clone https://github.com/versx/iPhoneController -b sqlite`  
3. `cd iPhoneController`  
4. `~/.dotnet/dotnet build`  
5. `cp config.example.json bin/Debug/netcoreapp2.1/config.json`  
6. `cd bin/Debug/netcoreapp2.1`  
7. `nano config.json` / `vi config.json` (Fill out config)  
8. `~/.dotnet/dotnet iPhoneController.dll`  