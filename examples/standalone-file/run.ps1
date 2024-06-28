#! /usr/bin/env pwsh
dotnet publish ../../src/Xiangyao/Xiangyao.csproj -c Release -o ./app

./app/Xiangyao --provider=file
