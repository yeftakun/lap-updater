# LapUpdater

<img width="1073" height="506" alt="image" src="https://github.com/user-attachments/assets/3cdef1b5-0a13-49ff-8abf-35ddb2932401" />

A small Windows Forms utility to sync Assetto Corsa `personalbest.ini` into your GitHub-hosted lap board. It copies your selected `personalbest.ini` into the repo's `data/` folder, checks for local/remote changes, and can commit/push updates with one click. Paths to the source INI and repo root are remembered between runs.

Download [LapUpdater](https://github.com/yeftakun/lap-updater/releases)

> Build for [yeftakun/ac-lapboard](https://github.com/yeftakun/ac-lapboard)

## Published Requirements
Build command (framework-dependent, assumes .NET 8 installed on target):
  ```
  dotnet publish "{root_path}\LapUpdater.csproj" -c Release --self-contained false -p:PublishSingleFile=false
  ```
- Output folder after publish: `{root}/bin/Release/net8.0-windows/publish/`
- Target OS: Windows x64
- Runtime: .NET 8 must be installed on the machine where you run the published exe.
