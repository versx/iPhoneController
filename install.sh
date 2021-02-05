# Install Homebrew if not already installed
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"

# Update Homebrew
brew update

# Uninstall ios-deploy from npm if already
sudo npm uninstall -g ios-deploy

# Install ios-deploy via Homebrew by running
brew install ios-deploy

# Uninstall libimobiledevice if already installed
brew uninstall --ignore-dependencies libimobiledevice

# Uninstall usbmux daemon
brew uninstall --ignore-dependencies usbmuxd

# Install latest usbmux daemon
brew install --HEAD usbmuxd

# Unlink libplist
brew unlink libplist

# Install latest libplist
brew install --HEAD libplist

# Install latest libimobiledevice
brew install --HEAD libimobiledevice

# Install latest idevice installer
brew install --HEAD ideviceinstaller

# Recreate ideviceinstaller link
brew unlink ideviceinstaller && brew link ideviceinstaller

# Allow Execute, Read, and Write permissions on lockdown folder
sudo chmod -R 777 /var/db/lockdown/

# Install megatools  
brew install megatools  

# Download .NET Core 2.1 installer
wget https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.sh

# Make installer executable
chmod +x dotnet-install.sh

# Install .NET Core 2.1.0
./dotnet-install.sh --version 2.1.803

# Delete .NET Core 2.1.0 installer
rm dotnet-install.sh

# Clone repository
git clone https://github.com/versx/iPhoneController

# Change directory into cloned repository
cd iPhoneController

# Build iPhoneController.dll
~/.dotnet/dotnet build

# Copy example config
cp config.example.json bin/config.json

# Copy SAM pogo profile
cp sam_pogo.mobileconfig bin/sam_pogo.mobileconfig

# Change directory into build folder
#cd bin/Debug/netcoreapp2.1

# Start iPhoneController.dll
#~/.dotnet/dotnet iPhoneController.dll
