echo off
del /f /q /s bin\nuget
mkdir bin\nuget

nuget pack Flumine\Flumine.csproj -outputDirectory bin\nuget  -IncludeReferencedProjects
nuget pack Flumine.Mongodb\Flumine.Mongodb.csproj -outputDirectory bin\nuget  -IncludeReferencedProjects

echo Press enter to publish packages

:choice
set /P c=Packages ready. Are you ready to publish them [Y/N]?
if /I "%c%" EQU "Y" goto :publish
if /I "%c%" EQU "N" exit
goto :choice

:publish
nuget.exe push bin\nuget\*.nupkg
pause