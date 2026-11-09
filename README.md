# MelonModBuddy
MSBuild utilities for MelonLoader mod projects.

## Features
- Mod info class generation
- Copying to Mods Directory
- Thunderstore package generation

## Usage Guide
MelonModBuddy relies on some MSBuild properties and item groups to be set.

| Property                  | Description                                                                                                             | Default Value                 |
|---------------------------|-------------------------------------------------------------------------------------------------------------------------|-------------------------------|
| `GamePath`                | The path to the directory of the game you are modding.                                                                  |
| `ModName`                 | Name of your mod.                                                                                                       |                               |
| `ModAuthor`               | Author of your mod.                                                                                                     |                               |
| `ModVersion`              | Version of your mod. Must follow SemVer (Major.Minor.Patch)                                                             | `0.1.0`                       |
| `ModId`                   | A (hopefully unique) identifier for your mod.                                                                           | `{ModAuthor}.{ModName}`       |
| `ModWebsite`              | Value to be placed in the `website_url` field of Thunderstore manifest.                                                 |                               |
| `ModDescription`          | Desctiption of your mod. Value placed in the `description` field of Thunderstore manifest.                              |                               |
| `ModIcon`                 | Filepath to a png file to be used as the icon for Thunderstore package                                                  | `$(SolutionDir)/icon.png`     |
| `ModReadme`               | Filepath to the a Markdown file to use as the README.md for Thunderstore package.                                       | `$(SolutionDir)/README.md`    |
| `ModChangelog`            | Filepath to the a Markdown file to use as the CHANGELOG.md for Thunderstore package.                                    | `$(SolutionDir)/CHANGELOG.md` | 
| `ModApplyMelonAttributes` | Whether to automatically generates the assembly-scoped attributes for MelonLoader mods. Should be `true`/`false`.       | `false`                       |
| `ModUseMelonId`           | Whether to include the `MelonID` MelonLoader attribute automatically.                                                   | `false`                       |
| `MelonGameDeveloper`      | Value to be used as the developer field for the `MelonGame` attribute. Not used unless `MelonGameName` is also defined. |                               | 
| `MelonGameName`           | Value to be used as the game field for the `MelonGame` attribute. Not used unless `MelonGameDeveloper` is also defined. |                               |
| `CopyToModsFolder`        | Whether to copy the produced assembly, documentation, and debug files to the mods directory in the game installation.   | `false`                       |
| `ThunderstorePackage`     | Whether to create a Thunderstore package ready for upload after building.                                               | `false`                       |

| Item Group        | Description                                                                              |
|-------------------|------------------------------------------------------------------------------------------|
| `ModDependencies` | The dependency strings to include in the `dependencies` field in Thunderstore manifests. |

### User-specific Properties
Properties like `GamePath` are specific to the user and shoudln't be directly included in the project file. 
I highly recommend using a `.csproj.user` file to define this property, as it's ignored in common .gitignore templates.

1. Create a `{YourProjectName}.csproj.user` file in your projecy (i.e. it should be in the same directory) as your normal project file (`{YourProjectName}.csproj`).
2. Then include it in the main `csproj` file by adding this:
```xml
    <ItemGroup>
      <None Include="Millzy.MelonModBuddy.Tests.csproj.user" />
    </ItemGroup> 
```
3. In the `.csproj.user` file, set the `GamePath` property like you would in your project file.
```xml
<Project>
    <PropertyGroup>
        <GamePath>C:\Path\To\The\Game\Install\Directory\</GamePath>
        <!-- Any other user specific stuff can and should go in here too! -->
    </PropertyGroup>
</Project>
```

### Required Properties
In general, `ModName`, `ModVersion`, and `ModAuthor` **MUST** be set, otherwise the source generator **WILL** throw errors.

**If you set `ThunderstorePackage` to `true`:** You **MUST**:
- Make sure `ModIcon` and `ModReadme` contain valid paths to files;
- `ModName`, `ModVersion`, `ModWebsite`, and `ModDescription` are properly set and in the allowed formats.

### Example
```xml
    <PropertyGroup>
        <ModName>TestModName</ModName>
        <ModVersion>0.6.0</ModVersion>
        <ModAuthor>Me</ModAuthor>
        <ModWebsite>https://example.com</ModWebsite>
        <ModDescription>My cool mod</ModDescription>
        
        <ModApplyMelonAttributes>true</ModApplyMelonAttributes>
        <ModUseMelonId>true</ModUseMelonId>
        <MelonGameDeveloper>StressLevelZero</MelonGameDeveloper>
        <MelonGameName>BONELAB</MelonGameName>
        
        <ModIcon>$(SolutionDir)/CHANGELOG.md</ModIcon>
        <ModReadme>$(SolutionDir)/README.md</ModReadme>
        <!-- \/ Optional \/ -->
        <ModChangelog>PATH TO CHANGELOG</ModChangelog>
    </PropertyGroup>

    <ItemGroup>
        <ModDependencies Include="Author-TestDependency1-1.0.0" />
        <ModDependencies Include="Author-TestDependency2-1.0.0" />
        <ModDependencies Include="Author-TestDependency3-1.0.0" />
    </ItemGroup>
    
    <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
        <CopyToModsFolder>true</CopyToModsFolder>
        <ThunderstorePackage>false</ThunderstorePackage>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)' == 'Release'">
        <CopyToModsFolder>true</CopyToModsFolder>
        <ThunderstorePackage>true</ThunderstorePackage>
    </PropertyGroup>
```

With this configuration:
- MelonLoader assembly-scoped attributes are automatically included in the build.
```csharp
// MelonAssemblyInfo.g.cs
using System;
using MelonLoader;
using Millzy.MelonModBuddy.Tests;

[assembly: MelonInfo(
    typeof(Mod), 
    Millzy.MelonModBuddy.Tests.ModInfo.Name,
    Millzy.MelonModBuddy.Tests.ModInfo.Version,
    Millzy.MelonModBuddy.Tests.ModInfo.Author
)]
[assembly: MelonID(Millzy.MelonModBuddy.Tests.ModInfo.Id)]
[assembly: MelonGame("StressLevelZero", "BONELAB")]
```
- The following `ModInfo` class is generated:
```csharp
namespace Millzy.MelonModBuddy.Tests {
    internal static class ModInfo {
        public const string Name = "TestModName";
        public const string Id = "Me.TestModName";
        public const string Author = "Me";
        public const string Version = "0.6.0";
    }
}
```
- The mod (and any xml and pdp files for it) are copied to the mods directory in both Debug and Release mode.
- A Thunderstore package zip file is created in Release mode.