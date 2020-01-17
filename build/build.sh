#!/bin/sh
if [ -z $1 ]
then
echo "Build what?"
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
cd ../appbox.AppContainer
dotnet publish -c $1 -r linux-x64
cp -arf bin/$1/netcoreapp2.2/linux-x64/publish/* ../build/bin/
cd ../build

echo "====Build Host===="
cd ../appbox.Host
dotnet publish -c $1 -r linux-x64
cp -arf bin/$1/netcoreapp2.2/linux-x64/publish/* ../build/bin/

echo "====Done===="