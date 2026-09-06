<#
.SYNOPSIS
  Regenerates lib/Interop.MSTSCLib.dll from the local mstscax.dll type library.

.DESCRIPTION
  The .NET SDK (dotnet build) cannot run the ResolveComReference task, so the
  interop assembly for the Microsoft RDP ActiveX control is generated once with
  TlbImp.exe from the Windows SDK and committed to the repo.

  Run this only when a newer Windows build adds interfaces you need.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$tlbimp = Get-ChildItem 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin' -Recurse -Filter TlbImp.exe |
  Sort-Object FullName -Descending | Select-Object -First 1
if (-not $tlbimp) { throw 'TlbImp.exe not found. Install the Windows SDK (.NET Framework tools).' }
$out = Join-Path $root 'lib\Interop.MSTSCLib.dll'
& $tlbimp.FullName "$env:SystemRoot\System32\mstscax.dll" /out:$out /namespace:MSTSCLib /machine:Agnostic /silent
if ($LASTEXITCODE -ne 0) { throw "TlbImp failed with exit code $LASTEXITCODE" }
Write-Host "Wrote $out ($((Get-Item $out).Length) bytes) from mstscax.dll $((Get-Item "$env:SystemRoot\System32\mstscax.dll").VersionInfo.FileVersion)"
