dotnet clean Example1_PublishingFails
dotnet publish Example1_PublishingFails --configuration Release --runtime win-x64 --self-contained true

dotnet clean Example2_PublishingSucceeds
dotnet publish Example2_PublishingSucceeds --configuration Release --runtime win-x64 --self-contained true

