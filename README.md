[![Build](https://github.com/versx/iPhoneController/workflows/.NET%20Core/badge.svg)](https://github.com/versx/iPhoneController/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/iPhoneController.svg)](https://github.com/versx/iPhoneController/releases/)
[![GitHub Contributors](https://img.shields.io/github/contributors/versx/iPhoneController.svg)](https://github.com/versx/iPhoneController/graphs/contributors/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

# iPhoneController  
Reboot, grab a screenshot, running iOS versions, kill specific running processes, or remove Pokemon Go from multiple devices all from Discord.  

## Commands  

| Command | Description |
| ------------- | ------------- |
| `list [machine_name]`  | Retrieve a list of devices from all machines or a specific one. |
| `iosver [machine_name]` | Retrieve a list of iOS versions running on devices for all machines or a specific one. |
| `screen iPhone1, iPhone2` | Take a screenshot of specific device(s). |
| `sam iPhone1, iPhone2` | Reapply Single App Mode profile for provided device(s). |
| `reopen iPhone1, iPhone2` | Send restart game request to device(s). |
| `reboot iPhone1, iPhone2` | Reboot specific device(s). |
| `shutdown iPhone1, iPhone2` | Shutdown specific device(s). |
| `resign https://mega.nz/file/yS7C#Dsh0lZDkk 1.33.0b1 iPhone1,iPhone2` | Download latest app, resign, and deploy to specified devices (leave blank or specify `All` for all devices connected to the machine) |
| `deploy iPhone1,iPhone2` | Deploy latest already signed app from releases folder to specific device(s). |
| `rm-pogo iPhone1,iPhone2` | Removes Pokemon Go from specific device(s). |
| `kill usbmuxd [machine_name]` | Kill a specific process such as `usbmuxd`.  |

**Notes:**  
- *Parameters in brackets `[ ]` are optional*  
- *When specifying device names, spaces between commas is supported. i.e `!reboot iPhone1, iPhone2`*  
- *Best used with the same bot token if deploying to multiple machines. Devices not found will be skipped when executing commands.*  

## Installation  

Run the following commands and fill out config.  
```
wget https://raw.githubusercontent.com/versx/iPhoneController/master/install.sh && chmod +x install.sh && ./install.sh && rm install.sh
```

### App Deployment  
After building `iPhoneController` for the first time:  
1. In your `bin` folder, create a `releases/config` folder  
1. Copy your GC `config.json` to the new `releases/config` folder  
1. In your `bin` folder, create a `profiles` folder  
1. Copy your mobile provisioning profile to the new `profiles` folder  

### Single App Mode  
In order to reapply the SAM profile, you'll need to do the following:  
1. Copy `sam_pogo.mobileconfig` to your `bin` folder  
1. In AC2, click on the `Apple Configurator 2` menu and choose `Install Automation Tools`.  
1. In AC2, click on the `Apple Configurator 2` menu and choose `Preferences` > `Organization` > click on your org and choose `Export Supervision Identity` at the bottom left.  
1. Move the .crt and .der files to your `bin` folder and rename them to `org.crt` and `org.der`.  

## Updating  
1. `git pull` (from root of folder)  
1. `~/.dotnet/dotnet build`  
1. `cd bin`  
1. `~/.dotnet/dotnet iPhoneController.dll`  

## Running  
From the `bin` folder type the following:  
`~/.dotnet/dotnet iPhoneController.dll`  

## Running with PM2 (recommended)
Once everything is setup and running appropriately, you can add this to PM2 ecosysetm.config.js so it automatically starts:
`
module.exports = {
  apps: [
    {
      name: "iPhoneController",
      script: "iPhoneController.dll",
      watch: false,
      cwd: "/path/to/iPhoneController/bin",
      interpreter: "/Users/you/.dotnet/dotnet",
      instances: 1,
      exec_mode: "fork",
      max_restarts: 250,
      restart_delay: 300
    }
  ]
}
`
Adjust path and interpreter as required.  

**PM2 PLEASE NOTE**
If you have useIosDeploy=false and the bot doesn't respond, check console.  If you get the following error or similar you will need to fix PM2 permissions, see below.

`1|iPhoneController  | 21:49 [ERROR] [BOT] User xxx tried executing command screen and unknown error occurred.
1|iPhoneController  | : System.NullReferenceException: Object reference not set to an instance of an object.
1|iPhoneController  |    at iPhoneController.Commands.PhoneControl.ScreenshotAsync(CommandContext ctx, String phoneNames) in /private/path/to/iPhoneController/src/Commands/PhoneControl.cs:line 115
`
###Fixing PM2 Permissions###
1. From Mac terminal
` cat ~/Library/LaunchAgents/pm2.admin.plist`

look for the following line and note the path

`<string>/usr/local/lib/node_modules/pm2/bin/pm2 resurrect</string>`

2. From Mac desktop, Click the Apple, Open System Preferences 
![image](https://user-images.githubusercontent.com/3146205/124825495-b8217200-df41-11eb-8f9a-b0f154ff12f2.png)
3. Click Security and Privacy, click the lock to make changes and enter your password
![image](https://user-images.githubusercontent.com/3146205/124825572-d38c7d00-df41-11eb-8185-64aff4b2b078.png)
4. On left, click full disk access then either click the plus and add the path OR open finder and navigate to the path and drag / drop into here
![image](https://user-images.githubusercontent.com/3146205/124825746-0a629300-df42-11eb-87d2-d4b73f71daa0.png)
 
If you've already started iPhoneController with PM2 you'll now want to delete and relaunch after the above changes, this will now let PM2 access the files needed to run cfgutil.

## FAQ
Q. How do I get the profile ID?  
A. Run `security find-identity -p codesigning`  

Q. I'm receiveing `{"Command":"list","Output":{},"Type"::CommandOutput","Devices":[]}` when `cfgutil --format JSON list` is run.  
A. Reinstall Apple Configurator 2 automation tools  

Q. I'm tring to reapply SAM but get error "failed to get ECID for device"?
A. You likely have useIosDeploy = true, you need this to be false to use cfgutil to support re-applying SAM 

## TODO  
- Localization  
