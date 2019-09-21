# Salvage File #
A utility for copying as much of a file as possible.

This program tries to copy all the chunks of a file. Any chunks that fail are replaced with zeros.

## Usage ##
```
Usage: SalvageFile [options] (source) (destination)
 Copies as much of a file as possible.
 Options:
 -b (number)       Size of byte buffer in bytes (default: 4096). Larger values can be used to speed up the copy process but at reduced accuracy
 -v                Print more information including % complete
 -q                Print errors only and suppress copy warning messages
 -h / --help       Print this help
```
