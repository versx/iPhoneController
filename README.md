[![Build](https://github.com/versx/iPhoneController/workflows/.NET%20Core/badge.svg)](https://github.com/versx/iPhoneController/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/iPhoneController.svg)](https://github.com/versx/iPhoneController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/iPhoneController.svg)](https://github.com/versx/iPhoneController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

# iPhoneController  
Reboot, grab a screenshot, running iOS versions, kill specific running processes, or remove Pokemon Go from multiple devices all from Discord.  

## Commands  
- `list [machine_name]`  
  Retrieve a list of devices from all machines or a specific one.  
- `iosver [machine_name]`  
  Retrieve a list of iOS versions running on devices for all machines or a specific one.  
- `screen iPhone1, iPhone2`  
  Take a screenshot of specific devices.  
- `reopen iPhone1,iPhone2`  
  Send restart game request to device IP address.  
- `reboot iPhone1,iPhone2`  
  Reboot specific devices.  
- `shutdown iPhone1,iPhone2`  
  Shutdown specific devices.  
- `resign  https://mega.nz/file/yS7C#Dsh0lZDkk 1.33.0b1 iPhone1,iPhone2`  
  Download latest app, resign, and deploy to specified devices (leave blank or specify `All` for all devices connected to the machine)  
- `deploy iPhone1,iPhone2`  
  Deploy latest already signed app from releases folder to specific devices.  
- `rm-pogo iPhone1,iPhone2`  
  Removes Pokemon Go from specific devices.  
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
1. `brew uninstall --ignore-dependencies libimobiledevice`  
1. `brew uninstall --ignore-dependencies usbmuxd`  
1. `brew install --HEAD usbmuxd`  
1. `brew unlink usbmuxd`  
1. `brew link usbmuxd`  
1. `brew install --HEAD libimobiledevice`  
1. `brew unlink libimobiledevice && brew link libimobiledevice`  
1. `brew install --HEAD ideviceinstaller`  
1. `brew unlink ideviceinstaller && brew link ideviceinstaller`  
1. `sudo chmod -R 777 /var/db/lockdown/`  

__ios-deploy__  
If you have previously installed ios-deploy via npm, uninstall it:  
1. `sudo npm uninstall -g ios-deploy`  
Install ios-deploy via Homebrew by running:  
1. `brew install ios-deploy`  

__MegaTools__  
1. `brew install megatools`  

### iPhoneController  
**Installation script:** (Run the following commands, fill out config, skip to Running section)  
```
wget https://raw.githubusercontent.com/versx/iPhoneController/master/install.sh && chmod +x install.sh && ./install.sh && rm install.sh
```

**Manually:**  
1. `wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh --version 2.1.803 && rm dotnet-install.sh`  
1. `git clone https://github.com/versx/iPhoneController`  
1. `cd iPhoneController`  
1. `~/.dotnet/dotnet build`  
1. `cp config.example.json bin/config.json`  
1. `cd bin`  
1. `nano config.json` / `vi config.json` (Fill out config)  
1. `~/.dotnet/dotnet iPhoneController.dll`  

## App Deployment  
After building `iPhoneController` for the first time:  
1. In your `bin` folder, create a `releases/config` folder  
1. Copy your GC `config.json` to the new `releases/config` folder  
1. In your `bin` folder, create a `profiles` folder  
1. Copy your mobile provisioning profile to the new `profiles` folder  

## Updating  
1. `git pull` (from root of folder)  
1. `~/.dotnet/dotnet build`  
1. `cd bin`  
1. `~/.dotnet/dotnet iPhoneController.dll`  

## Running  
From the `bin` folder type the following:  
`~/.dotnet/dotnet iPhoneController.dll`  

## TODO  
- Localization  
