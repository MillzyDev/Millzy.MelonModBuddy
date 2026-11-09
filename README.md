# MelonModBuddy
MSBuild utilities for MelonLoader mod projects.

## Features
- Mod metadata class generation
- Copying to Mods Directory (both PC and Android systems)
- Assembly-scope attribute generation
- Thunderstore package generation

## Usage Guide
### Installation
TODO

### Mod Metadata Class Generation
This generates a static class containing constant values holding the name, author, version, and ID of your mod. 
It's intended to be used for any metadata-consuming attributes such as `MelonInfo` and `MelonId`, for example:
```cs
[assembly: MelonInfo(typeof(Mod), ModInfo.Name, ModInfo.Version, ModInfo.Author)]
[assembly: MelonID(ModInfo.Id)]
```

These values are controlled through the MSBuild properties: `ModName`, `ModVersion`, `ModAuthor`, and `ModId`.
Unless explicitly set, `ModId` defaults to `$(ModAuthor).$(ModVersion)`.

The default namespace and name of the generated class can be overridden, by setting `ModInfoNamespace` and `ModInfoClassName` respectively.
If not defined, they resolve to `$(RootNamespace)` and `ModInfo` respectively.

#### Example
```xml
<PropertyGroup>
    <ModName>MyAwesomeMod</ModName>
    <ModVersion>1.0.0</ModVersion>
    <ModAuthor>Me</ModAuthor>
</PropertyGroup>
```
Generates the following class:
```csharp
internal static class ModInfo {
    public const string Name = "MyAwesomeMod";
    public const string Id = "Me.MyAwesomeMod";
    public const string Author = "Me";
    public const string Version = "1.0.0";
}
```

### Copy To Mods Folder
This copies the project's main assembly, documentation files, and debug files to the MelonLoader mods folder, 
relative to the game's installation directory, which must be defined in the `GamePath` property. As the value of this 
install location for a game can differ per system, it is recommended you define this in a 
[user file](#user-specific-properties) or another alternative like an environment variable.

To enable this feature, set the property `CopyToModsFolder` to `true`
```xml
<PropertyGroup>
    <CopyToModsFolder>true</CopyToModsFolder>
</PropertyGroup>
```

### ADB Push To Mods Folder
This functions as a [Copy To Mods Folder](#copy-to-mods-folder) for Android devices (except documentation files are not 
copied). It requires ADB to be installed and findable in one of the directories listed in the `PATH` environment 
variable, and for ADB to be able to establish a connection (i.e. available via USB debugging, or wirelessly using 
`adb connect` prior to building)

You must set the package name for your target game in the `AndroidGamePackageName` property, as this is used for
constructing the path to the mod's folder, and this feature must be enabled by setting `AdbPushToModsFolder` to `true`:
```xml
<PropertuGroup>
    <AdbPushToModsFolder>true</AdbPushToModsFolder>
    <AndroidGamePackageName>com.StressLevelZero.BONELAB</AndroidGamePackageName>
</PropertuGroup>
```

If configuring your project to build and iterate on multiple systems, I recommend setting up separate build 
configurations for PC and Android.

### Assembly-scope Attribute Generation
This is a source generator that automatically produces MelonLoader assembly-scope attributes in the build.
This uses the metadata class produced by [mod metadata class generation](#mod-metadata-class-generation).

Enable this feature by setting the `ModApplyMelonAttributes` property to `true`:
```xml
<PropertyGroup>
    <ModApplyMelonAttributes>true</ModApplyMelonAttributes>
</PropertyGroup>
```
```csharp
[assembly: MelonInfo(typeof(Mod), ModInfo.Name, ModInfo.Version, ModInfo.Author)]
```

If your `MelonMod` class is not named `Mod`, it can be overridden by setting the value of `ModClassName` to match the
class' name relative to `$(RootNamespace)`

By default, this will only generate the `MelonInfo` attribute, but it can also generate `MelonID` and `MelonGame` 
attributes. `MelonID` generation can be enabled by setting `ModUseMelonId` to `true`, and `MelonGame` generation can be
enabled by setting values for `MelonGameDeveloper` and `MelonGameName`:
```xml
<PropertyGroup>
    <ModApplyMelonAttributes>true</ModApplyMelonAttributes>
    <ModUseMelonId>true</ModUseMelonId>
    <MelonGameDeveloper>StressLevelZero</MelonGameDeveloper>
    <MelonGameName>BONELAB</MelonGameName>
</PropertyGroup>
```
```csharp
[assembly: MelonInfo( typeof(Mod), ModInfo.Name, ModInfo.Version, ModInfo.Author)]
[assembly: MelonID(Millzy.MelonModBuddy.Tests.ModInfo.Id)]
[assembly: MelonGame("StressLevelZero", "BONELAB")]
```

## User-specific Properties
Properties like `GamePath` are specific to the user and shouldn't be directly included in the project file.
I highly recommend using a `.csproj.user` file to define this property, as it's ignored in common .gitignore templates
and usually automatically resolved by MSBuild.

1. Create a `{YourProjectName}.csproj.user` file in your project (i.e. it should be in the same directory) as your normal project file (`{YourProjectName}.csproj`).
2. In the `.csproj.user` file, set the `GamePath` property like you would in your project file.
```xml
<Project>
    <PropertyGroup>
        <GamePath>C:\Path\To\The\Game\Install\Directory\</GamePath>
        <!-- Any other user specific stuff can and should go in here too! -->
    </PropertyGroup>
</Project>
```

## Thunderstore Package Generation
This generates a Thunderstore package ZIP in your build output that can be directly uploaded to Thunderstore. It uses 
the values of `ModName`, `ModVersion`, and `ModAuthor`, but requires the additional definition of: `ModDescription` and 
optionally `ModWebsite`.

```xml
<PropertyGroup>
    <ModName>TestModName</ModName>
    <ModVersion>0.6.0</ModVersion>
    <ModAuthor>Me</ModAuthor>
    <ModWebsite>https://example.com</ModWebsite>
    <ModDescription>My cool mod</ModDescription>
</PropertyGroup>
```

Dependencies on other mods must be specified by adding the dependency strings to the `ModDependencies` item:
```xml
<ItemGroup>
    <ModDependencies Include="bonelib-BoneLib-3.2.2" />
    <ModDependencies Include="WNP78-FieldInjector-2.1.1" />
</ItemGroup>
```

Your project MUST have a `README.md` and an `icon.png` file to create a Thunderstore package. By default, the solution
folder is checked for them. The files to use for README.png and icon.png can be set in the `ModReadme` and `ModIcon`
properties. Note: these values do not have to path to files named `README.md` or `icon.png`, whatever file they end up 
pointing to is renamed when copying to the package.

A `CHANGELOG.md` file is also searched for, however the presence of this file is optional. The path to the changelog 
file can also be overridden by setting `ModChangelog`.

### Required Properties
In general, `ModName`, `ModVersion`, and `ModAuthor` **MUST** be set, otherwise the source generator **WILL** produce
errors.

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

## All Properties
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
| `ModInfoNamespace`        | Namespace to use for the generated ModInfo class.                                                                       | `$(RootNamespace)`            |
| `ModInfoClassName`        | Class name to use for the generated ModInfo class.                                                                      | `ModInfo`                     |
| `ModApplyMelonAttributes` | Whether to automatically generates the assembly-scoped attributes for MelonLoader mods. Should be `true`/`false`.       | `false`                       |
| `ModUseMelonId`           | Whether to include the `MelonID` MelonLoader attribute automatically.                                                   | `false`                       |
| `MelonGameDeveloper`      | Value to be used as the developer field for the `MelonGame` attribute. Not used unless `MelonGameName` is also defined. |                               | 
| `MelonGameName`           | Value to be used as the game field for the `MelonGame` attribute. Not used unless `MelonGameDeveloper` is also defined. |                               |
| `CopyToModsFolder`        | Whether to copy the produced assembly, documentation, and debug files to the mods directory in the game installation.   | `false`                       |
| `ThunderstorePackage`     | Whether to create a Thunderstore package ready for upload after building.                                               | `false`                       |
| `AndroidGamePackageName`  | Package name of the game on Android, systems usually in the form DOMAIN.COMPANY.PRODUCT (required when pushing via ADB) |                               |
| `AdbPushToModsFolder`     | Whether to run `adb push` commands to install your mod on Android systems. Should be `true`/`false`                     | `false`                       |

| Item Group        | Description                                                                              |
|-------------------|------------------------------------------------------------------------------------------|
| `ModDependencies` | The dependency strings to include in the `dependencies` field in Thunderstore manifests. |
