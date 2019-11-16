# Install brew if not already installed
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"

# Install libimobiledevice
brew update
brew uninstall --ignore-dependencies libimobiledevice
brew uninstall --ignore-dependencies usbmuxd
brew install --HEAD usbmuxd
brew unlink usbmuxd
brew link usbmuxd
brew install --HEAD libimobiledevice
brew unlink libimobiledevice && brew link libimobiledevice
brew install --HEAD ideviceinstaller
brew unlink ideviceinstaller && brew link ideviceinstaller
sudo chmod -R 777 /var/db/lockdown/

# Download .NET Core 2.1 installer
wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh

# Make installer executable
chmod +x dotnet-install.sh

# Install .NET Core 2.1
./dotnet-install.sh

# Delete .NET Core 2.1 installer
rm dotnet-install.sh

# Clone repository
git clone https://github.com/versx/iPhoneController -b sqlite

# Change directory into cloned repository
cd iPhoneController

# Build iPhoneController.dll
~/.dotnet/dotnet build

# Copy example config
cp config.example.json bin/Debug/netcoreapp2.1/config.json

# Change directory into build folder
cd bin/Debug/netcoreapp2.1

# Start iPhoneController.dll
#~/.dotnet/dotnet iPhoneController.dll