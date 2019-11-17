# iPhoneController  
Reboot, grab a screenshot, view latest debug or full logs, remove Pokemon Go or UIC all from Discord.  

## Commands  
- `list` - Retrieve a list of all available devices.  
- `screen` - Take a screenshot of specific devices.  
- `reboot` - Reboot specific devices.  
- `rm-pogo` - Removes Pokemon Go from specific devices.  
- `rm-uic` - Removes UIC from specific devices.  
- `log-clear` - Delete all logs in the Logs folder.  
- `log-full` - Retrieve the latest full log of a device.  
- `log-debug` - Retrieve the latest debug log of a device.  
- `kill` - Kill a specific process such as `usbmuxd`.  

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


**From the installation script:** (Just need to fill out config and run)  
```
wget https://raw.githubusercontent.com/versx/iPhoneController/sqlite/install.sh && chmod +x install.sh && ./install.sh && rm install.sh
```

**Manually:**
- iPhoneController
1. `wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh && rm dotnet-install.sh`  
2. `git clone https://github.com/versx/iPhoneController -b sqlite`  
3. `cd iPhoneController`  
4. `~/.dotnet/dotnet build`  
5. `cp config.example.json bin/Debug/netcoreapp2.1/config.json`  
6. `cd bin/Debug/netcoreapp2.1`  
7. `nano config.json` / `vi config.json` (Fill out config)  
8. `~/.dotnet/dotnet iPhoneController.dll`  

## Updating  
1. `git pull`  
2. `~/.dotnet/dotnet build`  
3. `cd bin/Debug/netcoreapp2.1`  
4. `~/.dotnet/dotnet iPhoneController.dll`  