dotnet build ./contract
dotnet build C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj

# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- reset -f
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- contract deploy ./contract/bin/Debug/netstandard2.1/safe-purchase.nef genesis
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- transfer gas 10000 genesis buyer
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- transfer gas 10000 genesis seller
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- checkpoint create checkpoints/create-sale -f
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- contract invoke ./invoke-files/create-sale.neo-invoke.json seller
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- checkpoint create checkpoints/buyer-deposit -f
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- contract invoke ./invoke-files/buyer-deposit.neo-invoke.json buyer
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- checkpoint create checkpoints/confirm-shipment -f
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- contract invoke ./invoke-files/confirm-shipment.neo-invoke.json seller
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- checkpoint create checkpoints/confirm-received -f
# dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- contract invoke ./invoke-files/confirm-received.neo-invoke.json buyer
