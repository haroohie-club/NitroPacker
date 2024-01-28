# NitroPacker
NitroPacker is an open source, cross-platform utility for packing and unpacking Nintendo DS ROMs. It also can apply ASM hacks to both ARM9 and overlay files.

## Authors
This code was written primarily by Ermelber and Gericom. Jonko has ported it to .NET 8.0 and made modifications to make it cross-platform.

## Prerequisites
* [devkitARM](https://devkitpro.org/wiki/Getting_Started) and [Make](https://www.gnu.org/software/make/) are required to apply ASM hacks.
  - Alternatively, one can opt to install [Docker](https://www.docker.com/) (and [WSL](https://learn.microsoft.com/en-us/windows/wsl/install) on Windows)
    instead.

## Packing and Unpacking
The primary purpose of NitroPacker is to pack and unpack NDS ROMs.

### Unpacking
To unpack a ROM with NitroPacker, run the following command:
```
NitroPacker unpack -r PATH/TO/ROM.nds -o PATH/TO/UNPACK/DIRECTORY -p PROJECT_NAME
```
Replace the capitalized arguments with:
* `PATH/TO/ROM.nds` &ndash; the path to the Nintendo DS ROM you want to unpack
* `PATH/TO/UNPACK/DIRECTORY` &ndash; the path to the directory you want to unpack the ROM to
* `PROJECT_NAME` &ndash; the name of the project file that will be created on unpack (can be anything but usually should be the name of the game)

This command will unpack all the game's files and place them in the specified directory. The ROM's file system will go in the `data` subdirectory, while
any overlays present will be placed in the `overlay` subdirectory. The main ARM9 and ARM7 binaries will be placed in the root directory. Additionally, the created project file contains a bunch of metadata about the ROM, such as the ROM header, the overlay table, and the banner.

### Packing
NitroPacker can only pack a directory that was previously unpacked with NitroPacker (or something structured exactly like that). To pack a ROM, run the following command:
```
NitroPacker pack -p PATH/TO/PROJECT_FILE.xml -r PATH/TO/OUTPUT_ROM.nds
```

The project file is expected to be in a directory with ARM9 and ARM7 binaries as well as the data and overlay subdirectories mentioned previously.

## ASM Hacks
NitroPacker can also be used as a powerful tool to apply assembly hacks, but this functionality is a bit more complicated.

### Install Prerequisites
As mentioned above, NitroPacker relies on other programs to help with the assembling your hacks, namely, devkitARM and Make. DevkitARM is distributed
by devkitPro and can be downloaded from their website. They have a graphical installer for Windows (for which you need only select the NDS workloads)
and a package manager for Mac/Linux (for which you can run the command `dkp-pacman -S nds-dev` to install devkitARM).

On Linux distros, Make can be installed from the package manager (e.g. `sudo apt install make`). On macOS, you can use [brew](https://formulae.brew.sh/formula/make). On Windows, the easiest way to install it is with Chocolatey, where the command is `choco install make`.

If either of these options presents a challenge for you or doesn't work for some reason, you can instead opt to use the alternate method of assembling
the code in Docker containers. After installing [Docker Desktop](https://www.docker.com/products/docker-desktop/) or the Docker engine, ensure the engine
is running. Then, when calling NitroPacker, ensure you pass `-d` followed by the Docker tag you want to use. If you're not sure which one to use, we
recommend trying `20230526` as that is the most recent tag as of this publishing (when the samples were last updated). If you want to use newer features
or need later bugfixes, choose `latest` or a later tag. See [devkitpro/devkitarm on DockerHub](https://hub.docker.com/r/devkitpro/devkitarm) for more details.

### Directory Structure
You will need to move the unpacked `arm9.bin` to a different directory with a specific structure:

* `src`
  - `overlays`
    - `main_HEXOVERLAYNUM` &ndash; the name here corresponds to the name of the overlay in the unpacked `overlay` directory
      - `replSource` &ndash; a directory containing subdirectories with instructions to be directly replaced in the specified overlay
        - `HEXLOCATION` &ndash; the location of an instruction to replace
          - `HEXLOCATION.s` &ndash; a source file containing an instruction to replace at the specified `HEXLOCATION`
      - `source` &ndash; a directory containing source files to append to the specified overlay
        - `file.s` &ndash; ARM assembly source files that will be assembled and appended to the overlay
        - `file.c` &ndash; C source files that will be assembled and appended to the overlay
        - `file.cpp` &ndash; C++ source file that will be assembled and appended to the overlay
      - `symbols.x` &ndash; an editable symbols file that allows for naming specific offsets for use in specified overlay's source files
    - `linker.x` &ndash; boilerplate linker file for overlays
    - `Makefile` &ndash; boilerplate Makefile for overlays
  - `replSource` &ndash; a directory containing subdirectories with instructions to be directly replaced in the ARM9
    - `HEXLOCATION` &ndash; the location of an instruction to replace
      - `HEXLOCATION.s` &ndash; a source file containing an instruction to replace at the specified `HEXLOCATION`
  - `source` &ndash; a directory contianing source files to append to the ARM9
    - `file.s` &ndash; ARM assembly source files that will be assembled and appended to the ARM9
    - `file.c` &ndash; C source files that will be assembled and appended to the ARM9
    - `file.cpp` &ndash; C++ source file that will be assembled and appended to the ARM9
  - `arm9.bin` &ndash; the main ARM9 &ndash; must be copied in from the unpacked ROM directory
  - `.clang-format` &ndash; boilerplate CLang format file
  - `linker.x` &ndash; boilerplate linker file for ARM9
  - `Makefile` &ndash; boilerplate Makefile for ARM9
  - `symbols.x` &ndash; an editable symbols file that allows for naming specific offsets for use in ARM9 source files

This directory structure should be copied from the `asm_sample` directory in this repo. A fully fleshed-out example can be found in the[ChokuretsuTranslationBuild repository](https://github.com/haroohie-club/ChokuretsuTranslationBuild/tree/main/src).

Additionally, prior to assembling the ARM9, you will need to find the arena lo offset. This value can be found by searching for `0x37F8000` in the ROM.
IDA or Ghidra should be used to search for this value. Once you find it, you'll want to go back a few values until you see an offset that has a value
between 0x20C0000 and 0x20F0000 &ndash; the offset (not the value) is the arena lo offset. It will most likely be followed by two subroutines that look like:

```arm
MOV             R0, R0,LSL#2
ADD             R0, R0, #0x2700000
ADD             R0, R0, #0xFF000
STR             R1, [R0,#0xDC4]
BX              LR

...

MOV             R0, R0,LSL#2
ADD             R0, R0, #0x2700000
ADD             R0, R0, #0xFF000
STR             R1, [R0,#0xDA0]
BX              LR
```

Finally, source files may have any valid ARM assembly. To hook into a function and create a branch link to your code, the following format should be used:

```arm
ahook_02061344:
   CODE_HERE
   bx lr
```

This method will replace the instruction at 0x02061344 with a BL to your code.

Once this directory has been constructed, source files have been created, the arena lo offset determined, and the ARM9 has been copied in, the following commands may be used:

### Assemble ARM9
To assemble the hacked ARM9, run the following command:
```
NitroPacker patch-arm9 -i PATH/TO/SRC/DIRECTORY -o PATH/TO/OUTPUT/DIRECTORY -a ARENA_LO_OFFSET [-d DOCKER_TAG]
```

This will assemble the hacks and append them to `arm9.bin`, then copy the ARM9 to the output directory (which should be the directory you will then run
the `pack` command on).

### Assemble Overlays
To patch the overlays, run the following command:
```
NitroPacker patch-overlays -i PATH/TO/ORIGINAL/OVERLAY/DIRECTORY -o PATH/TO/PATCHED/OVERLAY/DIRECTORY -s PATH/TO/OVERLAY/SOURCE -r PATH/TO/PROJECT/FILE [-d DOCKER_TAG]
```
