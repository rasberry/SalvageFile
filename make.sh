#!/bin/bash

# override commands which have pesky $path issues
find() {
	"/d/Software/dev/cmder/vendor/git-for-windows/usr/bin/find.EXE" "$@"
}
7z() {
	"/d/Software/disk/7-Zip/7z.exe" "$@"
}

_toLower() {
	echo "$1" | tr '[:upper:]' '[:lower:]'
}

_getrid() {
if [[ "$OSTYPE" == "linux"* ]]; then
	echo "linux-x64"
elif [[ "$OSTYPE" == "darwin"* ]]; then
	echo "osx-x64"
elif [[ "$OSTYPE" == "cygwin" ]]; then
	echo "linux-x64"
elif [[ "$OSTYPE" == "msys" ]]; then
	echo "win-x64"
elif [[ "$OSTYPE" == "win32" ]]; then
	echo "win-x64"
elif [[ "$OSTYPE" == "freebsd"* ]]; then
	echo ""
else
	echo ""
fi
}

_publishone() {
	if [ -z "$1" ]; then echo "_publishone invalid rid"; exit 1; fi
	if [ -z "$2" ]; then echo "_publishone invalid version"; exit 1; fi
	# if [ -z "$3" ]; then echo "_publishone framework"; exit 1; fi

	if [ ! -f "publish" ]; then mkdir "publish"; fi

	list="make.sh.tmp.txt"

	if [ -f "src/SalvageFile.csproj.orig" ]; then
		mv "src/SalvageFile.csproj.orig" "src/SalvageFile.csproj"
	fi

	# do a restore with RID
	dotnet restore -r "$1" --force-evaluate

	# build regular
	outNormal="bin/Normal/$1"
	dotnet build -c Release -r "$1" -o "$outNormal" "src/SalvageFile.csproj"

	# build standalone
	outAlone="bin/Alone/$1"
	dotnet publish -c Release --self-contained -r "$1" -o "$outAlone" "src/SalvageFile.csproj"

	# build native - TODO currently does not support cross-compiling
	outNative="bin/Native/$1"
	mv "src/SalvageFile.csproj" "src/SalvageFile.csproj.orig"
	cp "src/SalvageFile.csproj.native" "src/SalvageFile.csproj"
	dotnet publish -c Release -r "$1" -o "$outNative" "src/SalvageFile.csproj"
	mv "src/SalvageFile.csproj.orig" "src/SalvageFile.csproj"

	# package regular build
	find "./src/$outNormal/" -maxdepth 1 -type f > "$list"
	7z a -mx=9 -ms=on -i@"$list" "./publish/$1-$2.7z"

	# package standalone build
	find "./src/$outAlone/" -maxdepth 1 -type f > "$list"
	7z a -mx=9 -ms=on -i@"$list" "./publish/$1-standalone-$2.7z"

	# package native build
	find "./src/$outNative/" -maxdepth 1 -type f > "$list"
	7z a -mx=9 -ms=on -i@"$list" "./publish/$1-native-$2.7z"

	rm "$list"
}

# =============================================================================

test() {
	dotnet test
}
debug() {
	dotnet build -c "Debug"
}
release() {
	dotnet build -c "Release"
}
clean() {
	if [ -d "src/bin" ]; then
		rm -r "src/bin";
	fi
	if [ -d "src/obj" ]; then
		rm -r "src/obj";
	fi
	if [ -d "publish" ]; then
		rm -r "publish";
	fi

	if [ -f "src/SalvageFile.csproj.orig" ]; then
		mv "src/SalvageFile.csproj.orig" "src/SalvageFile.csproj"
	fi
}

publish() {
	# https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
	rid="$(_getrid)"

	if [ -n "$rid" ]; then
		_publishone "$(_getrid)" "0.1.0" "netcoreapp2.0"
	else
		echo "RID '$OSTYPE' not recognized"
	fi
}

if [ -z "$1" ]; then
	TARGET=debug
else
	TARGET=$(_toLower "$1")
fi

2>/dev/null $TARGET || (echo "Target \"$TARGET\" doesn't exist"; exit 1)
