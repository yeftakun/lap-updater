# LapUpdater

A small Windows Forms utility to sync Assetto Corsa `personalbest.ini` into your GitHub-hosted lap board. It copies your selected `personalbest.ini` into the repo's `data/` folder, checks for local/remote changes, and can commit/push updates with one click. Paths to the source INI and repo root are remembered between runs.

## Published Requirements
Build command (framework-dependent, assumes .NET 8 installed on target):
  ```
  dotnet publish "{root}\LapUpdater.csproj" -c Release --self-contained false -p:PublishSingleFile=false
  ```
- Output folder after publish: `{root}/bin/Release/net8.0-windows/publish/`
- Target OS: Windows x64
- Runtime: .NET 8 must be installed on the machine where you run the published exe.
