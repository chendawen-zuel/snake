$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$executable = Join-Path $outputDirectory 'SnakeClub.exe'
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/out:$executable" (Join-Path $PSScriptRoot 'SnakeClub.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination $outputDirectory
Compress-Archive -LiteralPath $executable,(Join-Path $outputDirectory 'README.txt') -DestinationPath (Join-Path $outputDirectory 'SnakeClub-Windows.zip') -Force
Write-Output $executable
