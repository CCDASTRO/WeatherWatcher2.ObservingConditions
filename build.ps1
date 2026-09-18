$ErrorActionPreference = 'Stop'
$msbuild = Join-Path ${env:ProgramFiles} 'Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'
$root = $PSScriptRoot
if (!(Test-Path (Join-Path $root "SafetyMonitor\ASCOMDriverTemplate.snk"))) { throw "Place your existing SafetyMonitor ASCOMDriverTemplate.snk in SafetyMonitor locally before building. Do not commit signing material." }
& $msbuild (Join-Path $root 'SafetyMonitor\SNFSafety.vbproj') /p:Configuration=Release /p:Platform=x86 /p:RegisterForComInterop=false /p:BaseIntermediateOutputPath=objRainCloud\ /v:minimal /nologo
if ($LASTEXITCODE) { throw 'Safety build failed' }
& "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tests\CheckArchitecture.ps1') -AssemblyPath (Join-Path $root 'SafetyMonitor\bin\Release\ASCOM.WeatherWatcher.SafetyMonitor.dll')
if ($LASTEXITCODE) { throw 'SafetyMonitor architecture verification failed' }
& $msbuild (Join-Path $root 'WeatherWatcher2.ObservingConditions.sln') /restore /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE) { throw 'WeatherWatcher build failed' }
& $msbuild (Join-Path $root 'tests\AmbientTests.csproj') /restore /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE) { throw 'Tests build failed' }
& (Join-Path $root 'tests\bin\AmbientTests.exe')
if ($LASTEXITCODE) { throw 'Tests failed' }

& (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe') (Join-Path $root 'installer\WeatherWatcher-Suite.iss')
if ($LASTEXITCODE) { throw 'Installer build failed' }
