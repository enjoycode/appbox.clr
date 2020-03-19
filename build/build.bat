@echo off
if %1 == {} goto usage
if %1 == Debug goto okpara
if %1 == Release goto okpara
goto usage

:okpara
rd /q /s ..\appbox.AppContainer\bin\%1\netcoreapp3.1\win-x64\publish
dotnet publish -r win-x64 -c %1 ..\appbox.AppContainer\appbox.AppContainer.csproj
if errorlevel 1 goto error
ncbeauty --loglevel=Info --nopatch=True ..\appbox.AppContainer\bin\%1\netcoreapp3.1\win-x64\publish lib
if errorlevel 1 goto notool

rd /q /s ..\appbox.Host\bin\%1\netcoreapp3.1\win-x64\publish
dotnet publish -r win-x64 -c %1 ..\appbox.Host\appbox.Host.csproj
if errorlevel 1 goto error
ncbeauty --loglevel=Info --nopatch=True ..\appbox.Host\bin\%1\netcoreapp3.1\win-x64\publish lib

xcopy /E /I /Y "..\appbox.AppContainer\bin\%1\netcoreapp3.1\win-x64\publish" "bin_win"
xcopy /E /I /Y "..\appbox.Host\bin\%1\netcoreapp3.1\win-x64\publish" "bin_win"
goto end

:usage
echo "Usage: build Debug or build Release"
goto end

:notool
echo "Install NetCoreBeauty first!"
goto end

:error
echo "oops, some error!"
goto end

:end
