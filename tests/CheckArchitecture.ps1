param([Parameter(Mandatory=$true)][string]$AssemblyPath)
$ErrorActionPreference = 'Stop'
# Run with Windows PowerShell (.NET Framework); modern .NET returns None for this obsolete property.
$assembly = [Reflection.AssemblyName]::GetAssemblyName($AssemblyPath)
if ($assembly.ProcessorArchitecture -ne [Reflection.ProcessorArchitecture]::MSIL) { throw 'SafetyMonitor must be AnyCPU for dual-architecture registration' }
Write-Output ('Verified AnyCPU: ' + $assembly.Version)
