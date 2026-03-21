Get-ChildItem -Include bin,obj,TestResults -Recurse | Remove-Item -Force -Recurse -ErrorAction Ignore
dotnet restore --nologo --verbosity quiet
dotnet build --no-restore --configuration Release --nologo --verbosity quiet
dotnet pack ./AlexandreHtrb.WebSocketExtensions/AlexandreHtrb.WebSocketExtensions.csproj --nologo --verbosity quiet --configuration Release 
[void](([XML]$nugetCsprojXml = Get-Content ./AlexandreHtrb.WebSocketExtensions/AlexandreHtrb.WebSocketExtensions.csproj))
$versionName = $nugetCsprojXml.Project.PropertyGroup.PackageVersion
$filePath = "./AlexandreHtrb.WebSocketExtensions/bin/Release/AlexandreHtrb.WebSocketExtensions.${versionName}.nupkg"
Write-Host "Package generated at ${filePath}" -ForegroundColor DarkGreen
