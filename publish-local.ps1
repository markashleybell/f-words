param([Parameter(Mandatory=$true)][string]$OutputFolder)

dotnet publish .\f-words\f-words.fsproj -c release -o $OutputFolder
