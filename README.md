# SE Event Handler

A comprehensive Space Engineers plugin system for managing game events, encounters, factions, and custom spawning across Client, Dedicated Server, and Torch environments.

## Overview

SE Event Handler is a multi-platform plugin that provides advanced game management capabilities for Space Engineers. It consists of three main components that work together to provide a unified event handling system:

- **ClientPlugin**: Client-side functionality with configurable UI
- **DedicatedPlugin**: Dedicated server-side functionality  
- **TorchPlugin**: Torch server integration with advanced management interface

## Features

### Core Features
- **Event Management**: Monitor and control global events in Space Engineers
- **Encounter Management**: Track and manage global encounters with removal capabilities
- **Faction Management**: View detailed faction information, members, and reputation systems
- **Station Management**: Monitor trading stations, inventory, and configurations
- **Custom Spawning**: Advanced ship spawning system with player targeting
- **Real-time Monitoring**: Live updates of game state and statistics

### TorchPlugin Specific Features
- **Interactive GUI**: Multi-tab interface for comprehensive game management
- **Faction Reputation Editor**: Direct editing of player-faction relationships
- **Station Store Management**: View and monitor trading station inventories
- **Active Encounter Tracking**: Real-time monitoring of active global encounters
- **Custom Ship Spawner**: Enhanced cargo ship spawning with custom logic
- **Admin Commands**: Console commands for faction and station management

### Shared Components
- **Harmony Patching**: Runtime code modification system
- **Configuration Management**: Persistent configuration across all components
- **Logging System**: Comprehensive logging with multiple severity levels
- **Cross-Platform Compatibility**: Works across Client, Dedicated, and Torch environments

## Architecture

The project uses a shared codebase approach:

```
├── ClientPlugin/          # Client-side plugin
├── DedicatedPlugin/       # Dedicated server plugin  
├── TorchPlugin/          # Torch server plugin with GUI
└── Shared/               # Common functionality
    ├── Config/           # Configuration management
    ├── Logging/          # Logging system
    ├── Patches/          # Harmony patches
    └── Plugin/           # Core plugin interfaces
```

## Requirements

### Development Environment
- **Visual Studio 2017+** or **Visual Studio 2022**
- **.NET Framework 4.8.1**
- **Space Engineers** (for Client/Dedicated builds)
- **Torch Server** (for Torch builds)

### Dependencies
- **Lib.Harmony 2.3.3** - Runtime patching framework
- **Newtonsoft.Json 13.0.2** - JSON serialization (Torch/Dedicated only)
- **All Space Engineers assemblies** - Game integration

## Initial Setup

**Important**: This project requires manual assembly reference setup as automated dependency resolution is not yet implemented.

### 1. Configure Directory Paths

Edit `Directory.Build.props` and update the paths to match your installation:

```xml
<Project>
  <PropertyGroup>
    <!-- Path to Space Engineers client installation -->
    <Bin64>C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64</Bin64>
    
    <!-- Path to Space Engineers Dedicated Server -->
    <Dedicated64>C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineersDedicatedServer\DedicatedServer64</Dedicated64>
    
    <!-- Path to Torch Server installation -->
    <Torch>C:\Path\To\Your\Torch\Server</Torch>
  </PropertyGroup>
</Project>
```

### 2. Assembly Dependencies

Each project requires specific assemblies from their respective installations. The projects are pre-configured to reference assemblies from the paths defined in `Directory.Build.props`.

#### ClientPlugin Dependencies
The ClientPlugin requires **100+ assemblies** from the Space Engineers client installation, including:
- Core VRage assemblies (VRage.dll, VRage.Game.dll, etc.)
- Sandbox assemblies (Sandbox.Game.dll, Sandbox.Common.dll, etc.)
- Space Engineers assemblies (SpaceEngineers.Game.dll, etc.)
- System assemblies and dependencies

#### DedicatedPlugin Dependencies  
Similar to ClientPlugin but uses assemblies from the Dedicated Server installation.

#### TorchPlugin Dependencies
Requires assemblies from both Torch installation and Dedicated Server:
- All Torch-specific assemblies (Torch.dll, Torch.API.dll, etc.)
- Dedicated Server assemblies
- WPF assemblies for the management interface

### 3. Verify Assembly References

After configuring paths, ensure all assembly references resolve correctly:

1. Open the solution in Visual Studio
2. Check for missing references in each project
3. Verify that the paths in `Directory.Build.props` are correct
4. Rebuild the solution to confirm all dependencies are resolved

## Building the Project

### Build Configuration
The solution supports two build configurations:
- **Debug**: Development build with debugging symbols
- **Release**: Optimized production build

### Build Process
1. Open `EventHandler.sln` in Visual Studio
2. Select your target configuration (Debug/Release)
3. Build the entire solution or individual projects
4. Built assemblies will be automatically deployed using the deploy scripts

### Deployment
Each project includes automated deployment:
- **ClientPlugin**: Deploys to `%Bin64%\Plugins\Local\`
- **DedicatedPlugin**: Deploys to `%Dedicated64%\Plugins\Local\`  
- **TorchPlugin**: Deploys to Torch plugins directory

## Usage

### Client Plugin
- Automatically loads with Space Engineers client
- Access configuration via mod settings or in-game interface
- Provides client-side event handling and patches

### Dedicated Server Plugin
- Place in dedicated server plugins directory
- Runs automatically with server startup
- Provides server-side event management

### Torch Plugin
- Install through Torch plugin manager or manual placement
- Access management interface through Torch GUI
- Use admin commands for advanced management

### Admin Commands (Torch)
```
!cmd factions              # List all factions with details
!cmd stations              # List all stations with information  
!cmd stations gps [tag]    # Generate GPS coordinates for stations
!cmd addspacestation       # Add a new trading station
!cmd removespacestation    # Remove a trading station
```

### Management Interface (Torch)
The Torch plugin provides a multi-tab interface:
- **Factions**: View faction details, members, reputation management
- **Global Events**: Monitor active global events
- **Encounters**: Track and manage global encounters  
- **Custom Spawn**: Advanced ship spawning controls

## Development

### Key Components

#### Event Handling
- Global event monitoring and management
- Custom event triggers and responses
- Event state persistence

#### Encounter System
- Global encounter tracking
- Real-time encounter management
- Encounter removal capabilities

#### Faction Management
- Comprehensive faction data access
- Reputation system integration
- Member management

#### Custom Spawning
- Enhanced cargo ship spawning
- Player-targeted spawning
- Custom spawn group support

### Extending the Plugin
1. Add new functionality to the `Shared` project for cross-platform features
2. Implement platform-specific features in individual plugin projects
3. Use Harmony patches for game code modifications
4. Follow the existing logging and configuration patterns

## Configuration

### Client Configuration
- Configuration file: `EventHandler.cfg` in SE user data directory
- Supports runtime configuration changes
- Settings persist across game sessions

### Server Configuration  
- Configuration managed through plugin interfaces
- Torch configuration available through management UI
- Settings synchronized across server restarts

## Troubleshooting

### Common Issues

**Assembly Reference Errors**
- Verify paths in `Directory.Build.props` are correct
- Ensure all required installations are present
- Check that assembly versions match your game installation

**Plugin Not Loading**
- Verify deployment directory is correct
- Check plugin compatibility with game version
- Review log files for loading errors

**Missing Dependencies**
- Ensure all required assemblies are available
- Verify NuGet packages are restored
- Check for version conflicts

### Logs and Debugging
- Plugin logs are written to respective game/server log directories
- Debug builds include additional logging information
- Use the built-in logging system for troubleshooting

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes following the existing code style
4. Test across all supported platforms
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Check existing documentation and code comments
- Review the troubleshooting section

## Version History

- **v1.0.0**: Initial release with multi-platform support
  - Complete faction and station management
  - Global event and encounter handling
  - Custom spawning system
  - Torch GUI interface
