dotnet nxp3 reset -f
dotnet nxp3 contract deploy ./contract/bin/Debug/netstandard2.1/safe-purchase.nef genesis
dotnet nxp3 transfer gas 1000 genesis buyer
dotnet nxp3 transfer gas 1000 genesis seller
dotnet nxp3 checkpoint create checkpoints/createSale