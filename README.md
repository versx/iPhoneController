# iPhoneController  
Reboot, grab a screenshot, running iOS versions, view latest debug or full logs, kill specific running processes, clear logs folders, remove Pokemon Go or UIC from multiple devices all from Discord.  

## Commands  
- `list [machine_name]`  
  Retrieve a list of devices from all machines or a specific one.  
- `iosver [machine_name]`  
  Retrieve a list of iOS versions running on devices for all machines or a specific one.  
- `screen iPhone1, iPhone2`  
  Take a screenshot of specific devices.  
- `reboot iPhone1,iPhone2`  
  Reboot specific devices.  
- `shutdown iPhone1,iPhone2`  
  Shutdown specific devices.  
- `rm-pogo iPhone1,iPhone2`  
  Removes Pokemon Go from specific devices.  
- `rm-uic iPhone1, iPhone2`  
  Removes UIC from specific devices.  
- `log-clear [machine_name]`  
  Delete all logs in the Logs folder from all machines or a specific one.  
- `log-full iPhone1`  
  Retrieve the latest full log of a device.  
- `log-debug iPhone1`  
  Retrieve the latest debug log of a device.  
- `kill usbmuxd [machine_name]`  
  Kill a specific process such as `usbmuxd`.  

**Notes:**  
- *Parameters in brackets `[ ]` are optional*  
- *When specifying device names, spaces between commas is supported. i.e `!reboot iPhone1, iPhone2`*  
- *Best used with the same bot token if deploying to multiple machines. Devices not found will be skipped when executing commands.*  

## Installation  

### Prerequisites:  
__idevicediagnostics__  
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

__ios-deploy__  
If you have previously installed ios-deploy via npm, uninstall it:  
1. `sudo npm uninstall -g ios-deploy`  
Install ios-deploy via Homebrew by running:  
2. `brew install ios-deploy`  

__iPhoneController__  
**Installation script:** (Run the following commands, fill out config, skip to Running section)  
```
wget https://raw.githubusercontent.com/versx/iPhoneController/sqlite/install.sh && chmod +x install.sh && ./install.sh && rm install.sh
```

**Manually:**  
1. `wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh && rm dotnet-install.sh`  
2. `git clone https://github.com/versx/iPhoneController -b sqlite`  
3. `cd iPhoneController`  
4. `~/.dotnet/dotnet build`  
5. `cp config.example.json bin/Debug/netcoreapp2.1/config.json`  
6. `cd bin/Debug/netcoreapp2.1`  
7. `nano config.json` / `vi config.json` (Fill out config)  
8. `~/.dotnet/dotnet iPhoneController.dll`  

## Updating  
1. `git pull` (from root of folder)  
2. `~/.dotnet/dotnet build`  
3. `cd bin/Debug/netcoreapp2.1`  
4. `~/.dotnet/dotnet iPhoneController.dll`  

## Running  
From the `bin/debug/netcoreapp2.1` folder type the following:  
`~/.dotnet/dotnet iPhoneController.dll`  

## TODO  
- Add support for reinstalling UIC and Pokemon Go  
- Localization  
- Reinstall PoGo
