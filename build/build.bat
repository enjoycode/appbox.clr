dotnet publish -r win-x64 -c Debug ..\appbox.AppContainer\appbox.AppContainer.csproj
dotnet publish -r win-x64 -c Debug ..\appbox.Host\appbox.Host.csproj

xcopy /E /I /Y ..\appbox.AppContainer\bin\Debug\netcoreapp2.2\win-x64\publish bin\
xcopy /E /I /Y ..\appbox.Host\bin\Debug\netcoreapp2.2\win-x64\publish bin\