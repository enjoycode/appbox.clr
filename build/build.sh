#!/bin/sh
if [ -z $1 ]
then
echo "Usage: ./build.sh Debug or Release"
exit 2
fi

if [ $1 != Release ] && [ $1 != Debug ]
then
echo "Wrong build mode"
exit 2
fi

echo "====Delete old build===="
rm bin/appbox.*.*
rm bin/lib/appbox.*.*

echo "====Build AppContainer===="
rm -rf ../appbox.AppContainer/bin/$1/netcoreapp3.1/linux-x64/publish
dotnet publish -c $1 -r linux-x64 ../appbox.AppContainer
ncbeauty --loglevel=Info --nopatch=True ../appbox.AppContainer/bin/$1/netcoreapp3.1/linux-x64/publish lib
cp -arf ../appbox.AppContainer/bin/$1/netcoreapp3.1/linux-x64/publish/* bin/

echo "====Build Host===="
rm -rf ../appbox.Host/bin/$1/netcoreapp3.1/linux-x64/publish
dotnet publish -c $1 -r linux-x64 ../appbox.Host
ncbeauty --loglevel=Info --nopatch=True ../appbox.Host/bin/$1/netcoreapp3.1/linux-x64/publish lib
cp -arf ../appbox.Host/bin/$1/netcoreapp3.1/linux-x64/publish/* bin/

echo "====Done===="