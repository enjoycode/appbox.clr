@echo off
if %1 == {} goto host
if %1 == all goto all
echo "Usage: update [all]"
goto end

:all
dotnet build -c Debug ..\appbox.AppContainer\appbox.AppContainer.csproj
xcopy /I /Y "..\appbox.AppContainer\bin\Debug\netcoreapp2.2\appbox.AppContainer.dll" "bin_win"

:host
dotnet build -c Debug ..\appbox.Host\appbox.Host.csproj

xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Host.dll" "bin_win"
xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Core.dll" "bin_win\lib"
xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Design.dll" "bin_win\lib"
xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Server.dll" "bin_win\lib"
xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Store.dll" "bin_win\lib"
xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Store.PostgreSQL.dll" "bin_win\lib"
xcopy /I /Y "..\appbox.Host\bin\Debug\netcoreapp2.2\appbox.Store.Cassandra.dll" "bin_win\lib"

:end