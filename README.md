---
page_type: sample
languages:
- java
products:
- azure
services: Compute
platforms: dotnet
author: yaohaizh
---

# Getting started on managing virtual machines using vm extensions in C# #

          Azure Compute sample for managing virtual machine extensions. -
           - Create a Linux and Windows virtual machine
           - Add three users (user names and passwords for windows, SSH keys for Linux)
           - Resets user credentials
           - Remove a user
           - Install MySQL on Linux | something significant on Windows
           - Remove extensions


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/compute-dotnet-manage-virtual-machine-using-vm-extensions.git

    cd compute-dotnet-manage-virtual-machine-using-vm-extensions

    dotnet build

    bin\Debug\net452\ManageVirtualMachineExtension.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.