# Salvage File #
A utility for copying as much of a file as possible.

## Usage ##
```
Usage: SalvageFile (action)
 Common Options:
 -h / --help       Print this help

 (s)alvage (source) (destination)
 Copies as much of a file as possible.
  Options:
  -b (number)       Size of byte buffer in bytes (default: 4096). Larger values can be used to speed up the copy proces
   s but at reduced accuracy
  -v                Print more information including % complete
  -q                Print errors only and suppress copy warning messages

 (d)iff (file one) (file two)
 Compares the blocks of each file and shows if they are different
  Options:
  -b (number)       Size blocks in bytes (default: 4096)
  -v                Show all block hashes
  -q                Only show blocks that differ

```
## Notes ##
* This program tries to copy all the chunks of a file. Any chunks that fail are replaced with zeros.
* I've seen that sometimes using a smaller buffer (-b) results in *worse* recovery, so you may need to play with the setting to achieve best results.
* As far as I can tell there's no way to control the read timeout without using system-dependent calls (which i'm avoiding)
* This program uses the regular [System.IO.File](https://referencesource.microsoft.com/#mscorlib/system/io/file.cs) class to read / write files
* The source and destination file systems must support seeking (most do).

## Build ##
* Install dotnet. see [Get started](https://docs.microsoft.com/en-us/dotnet/core/get-started)
* Build with ```dotnet build -p src```

### Native Build ###
* Follow the [CoreRT - build prerequesites](https://github.com/dotnet/corert/blob/ebfbbcd99fac1746a8489a393a4873800c470ef3/Documentation/prerequisites-for-building.md)
* Build with ```./make.sh publish```

#### Linux ####
1. Install dotnet [on linux](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)
2. run ```./make.sh publish```

#### Windows ####
* Using Visual Studio
  1. Launch the "x64 Native Tools Command Prompt for VS" command prompt
  2. Use [msys](http://mingw.org/wiki/msys) bash to execute ```./make.sh publish```
* I've also had success using [cmder](https://cmder.net/) which comes with bash
* Installing dotnet on [WSL](https://docs.microsoft.com/en-us/windows/wsl/about) is rummored to work as well

#### Mac Os-X ####
* I don't have a Mac currently - TBD
* Presumably this is similar to linux. see [Prerequisites for macOS](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)

## TODO ##
* Test source and destination filesystem for seek support (currently just throws an exception)
* Check if destination file already exists and prompt for overwrite
* Consider implementing a sequential read option to allow reading non-seekable filesystems
* Add a way to diff copies of a recovered file and see which chunks are different
  * maybe show % amount of 0's in the chunk
* Maybe make a chunk 'merge' tool where you select which chunks to keep from each file
* output which chunks were replaced with 0's
