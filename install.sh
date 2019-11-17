# Install Homebrew if not already installed
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"

# Update Homebrew
brew update

# Uninstall libimobiledevice if already installed
brew uninstall --ignore-dependencies libimobiledevice

# Uninstall usbmux daemon
brew uninstall --ignore-dependencies usbmuxd

# Install latest usbmux daemon
brew install --HEAD usbmuxd

# Recreate usbmux daemon link
brew unlink usbmuxd && brew link usbmuxd

# Install latest libimobiledevice
brew install --HEAD libimobiledevice

# Recreate ilibmobiledevice link
brew unlink libimobiledevice && brew link libimobiledevice

# Install latest idevice installer
brew install --HEAD ideviceinstaller

# Recreate ideviceinstaller link
brew unlink ideviceinstaller && brew link ideviceinstaller

# Allow Execute, Read, and Write permissions on lockdown folder
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