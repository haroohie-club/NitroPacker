# NitroPacker 3.0 Breaking API Changes

## NuGet Structural Changes

First off: NitroPacker will now be available on [nuget.org](https://nuget.org/)! If you were previously pulling from the Azure Artifacts feed, you should now be able to safely remove that from your NuGet.config.

Furthermore, NitroPacker's package structure has been consolidated down to a single package: HaroohieClub.NitroPacker. You can replace the references to any other packages (including the .Core package) with that reference.

## API Changes

### Project Files
The `ProjectFile` class has been removed and is now simply the `NdsProjectFile` class. Additionally, many properties were renamed, changed, and broken out to better reflect the [gbatek documentation](https://mgba-emu.github.io/gbatek/).

### `NdsProjectFile` API Changes

| 2.x | 3.x |
|-----|-----|
| `FromByteArray` | Use `Deserialize` and pass the path of the project file rather than a byte array |
| `RomHeader`, `NitroFooter`, `RomOVT`, `RomBanner`, `RomFNT` | These classes are no longer subclasses of `Rom` and now exist in the top-level namespace |
| `RomOVT` | `RomOverlayTable` |
| `RSASignature` | `RsaSignature` |
| `RomFNT` | `RomFileNameTable` |

### `Rom` API Changes

| 2.x | 3.x |
|-----|-----|
| `MainRom` | `Arm9Binary` |
| `SubRom` | `Arm7Binary` |
| `Fnt` | `FileNameTable` |
| `MainOVT` | `Arm9OverlayTable` |
| `SubOVT` | `Arm7OverlayTable` |
| `RSASignature` | `RsaSignature` |

Additionally, the following properties representing various facets of DSi/DSi-enhanced ROMs have been added:

* `DigestSectorHashtableBinary`
* `DigestBlockHashtableBinary`
* `TwlStaticHeader`
* `Arm9iBinary`
* `Arm7iBinary`
* `DSiWareExtraData`

All of these are XML-documented and also on gbatek.

### `RomBanner` API Changes

`RomBanner` has been extended to contain all possible versions of the ROM banner: `BannerV1`, `BannerV2`, `BannerV3`, and `BannerV103`. Each banner contains a `Header` and then a `Banner` object representing its actual content. You can use the `Header`'s `Version` property to appropriately cast `Banner` to the proper class.

At the project level, banners are now stored as a separate binary rather than part of the project file. This allows them to be edited with third-party tools.

### `Banner` API Changes

| 2.x | 3.x |
|-----|-----|
| `Pltt` | `Palette` |
| `GetCrc` | `GetCrcs` &ndash; this method adds support for the longer CRCs of the later banner versions |

### `NitroFooter` API Changes

| 2.x | 3.x |
|-----|-----|
| `StartModuleParamsOffset` | `_start_ModuleParamsOffset` |

### `RomOverlayTable` API Changes

| 2.x | 3.x |
|-----|-----|
| `SinitInit` | `StaticInitializerStartAddress` |
| `SinitInitEnd` | `StaticInitializerEndAddress` |

### `RomHeader` API Changes

| 2.x | 3.x | Notes |
|-----|-----|-------|
| `byte ProductId` | `UnitCodes UnitCode` | The new `UnitCodes` enum gives the different unit code values names |
| `DeviceType` | `EncryptionSeedSelect` | |
| `DeviceSize` | `DeviceCapacity` | |
| `byte[] ReservedA` | `DSiFlags Flags` and `RegionOrPermitJump RegionOrJump` | These bytes were previously thought to be "reserved" but are now known to contain two different values which are broken out as seen |
| `GameVersion` | `RomVersion` | |
| `Property` | `AutoStart` | |
| `MainEntryAddress` | `Arm9EntryAddress` | |
| `MainRamAddress` | `Arm9RamAddress` | |
| `SubEntryAddress` | `Arm7EntryAddress` | |
| `SubRamAddress` | `Arm7RamAddress` | |
| `byte[] RomParamA` | `uint NormalCommandSettings` and `uint Key1CommandSettings` | |
| `byte[] RomParamB` | `ushort SecureAreaDelay` | |
| `MainAutoloadDone` | `Arm9AutoloadHookRamAddress` | |
| `SubAutoloadDone` | `Arm7AutoloadHookRamAddress` | |
| `RomParamC` | `SecureAreaDisable` | |
| `RomSize` | `RomSizeExcludingDSiArea` | |
| `ReservedB` | `Arm9ParametersTableOffset`, `Arm7ParametersTableOffset`, `DSiNTRRomRegionEnd`, `DSiTWLRomRegionStart`, and still some `ReservedB` | `ReservedB` is smaller than originally assumed, so it has been broken out into some more properties |
| `LogoData` | `NintendoLogoData` | |

Finally, many properties have been added as part of the `TwlHeader` class in property `DSiHeader` for DSi and DSi-enhanced games.
