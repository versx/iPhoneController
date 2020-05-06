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
1.) Run the following commands:  
```
wget https://raw.githubusercontent.com/versx/iPhoneController/sqlite/install.sh && chmod +x install.sh && ./install.sh && rm install.sh
```
2.) Edit and fill out `config.json`  
```
vi bin/debug/netcoreapp2.1/config.json
```
3.) Run iPhoneController  
```
~/.dotnet/dotnet bin/debug/netcoreapp2.1/iPhoneController.dll
```

## Updating  
1. `git pull` (from root of folder)  
2. `~/.dotnet/dotnet build`  
3. `~/.dotnet/dotnet bin/debug/netcoreapp2.1/iPhoneController.dll`  

## TODO  
- Add support for reinstalling UIC and Pokemon Go  
- Localization  
- Reinstall PoGo

## Discord  
https://discordapp.com/invite/zZ9h9Xa  
