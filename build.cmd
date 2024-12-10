echo on
if exist bin rmdir /s /q bin 
dotnet publish Hercules\Hercules.csproj /p:PublishProfile=Hercules\Properties\PublishProfiles\PublishProfile.pubxml