rdr2-defusal-dotnet (starter scaffold)
=====================================

This is a minimal ScriptHookRDR2DotNet-V2 project for Red Dead Redemption 2 Story Mode.

Folders
-------
- src/Rdr2Defusal/ : C# project
- libs/           : put ScriptHookRDRNetAPI.dll here (copy from your RDR2 root)
- dist/           : build output goes here

Setup
-----
1) Copy ScriptHookRDRNetAPI.dll into ./libs/
2) Build in VS Code terminal:

   dotnet build -c Release

3) Copy dist/Rdr2Defusal.dll into your RDR2 root scripts/ folder:

   <RDR2>/scripts/Rdr2Defusal.dll

4) Launch Story Mode. Press INSERT to reload .NET scripts if needed.
5) Press F8 in-game to ragdoll the player and show a subtitle.

Notes
-----
- Targets .NET Framework 4.8 (net48)
- PlatformTarget x64
