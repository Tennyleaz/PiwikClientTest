# get date to a folder
[string]$projectName = "ReportViewer "
[string]$today = Get-Date -UFormat "%Y%m%d"
[string]$currentDir = $PSScriptRoot  # This is an automatic variable set to the current file's/module's directory
[string]$folderName = $currentDir + "\" + $projectName + $today

write-host Make temp direcory ...
mkdir $folderName

write-host Copying files...
[string]$binDirDll = $PSScriptRoot + "\bin\Debug\*.dll";
[string]$binDirExe = $PSScriptRoot + "\bin\Debug\ReportViewer.exe";
[string]$binDirKey = $PSScriptRoot + "\bin\Debug\key.txt";
Copy-Item $binDirDll -Destination $folderName
Copy-Item $binDirExe -Destination $folderName
Copy-Item $binDirKey -Destination $folderName

write-host Make zip archive...
[string]$archiveFile = $currentDir + "\ReportViewer " + $today + ".zip"
Compress-Archive -Path $folderName -DestinationPath $archiveFile

write-host Delete unused files...
Remove-Item -path $folderName -Recurse

write-host "Press any key to continue..."
[void][System.Console]::ReadKey($true)