dotnet build "d:\code\lap-updater\lap-updater.sln" -c Release
dotnet publish "D:\code\lap-updater\LapUpdater.csproj" -c Release --self-contained false -p:PublishSingleFile=false