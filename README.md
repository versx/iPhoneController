# iPhoneController

## Installation

### Prerequisites:
- idevicediagnostics  

1. `wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh && rm dotnet-install.sh`  
2. `git clone https://github.com/versx/iPhoneController -b sqlite`  
3. `cd iPhoneController`  
4. `cp config.example.json bin/Debug/netcoreapp2.1/config.json`  
5. `nano config.json` / `vi config.json` (Fill out config)  
6. `~/.dotnet/dotnet build`  
7. `~/.dotnet/dotnet iPhoneController.dll`  