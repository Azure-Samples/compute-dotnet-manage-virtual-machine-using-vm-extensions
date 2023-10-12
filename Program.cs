// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information. 

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute.Models;

namespace ManageVirtualMachineExtension
{
    public class Program
    {
        // Linux configurations
        readonly static string FirstLinuxUserName = Utilities.CreateUsername();
        readonly static string FirstLinuxUserPassword = Utilities.CreatePassword();
        readonly static string FirstLinuxUserNewPassword = Utilities.CreatePassword();

        readonly static string SecondLinuxUserName = "seconduser";
        readonly static string SecondLinuxUserPassword = Utilities.CreatePassword();
        readonly static string SecondLinuxUserExpiration = "2020-12-31";

        readonly static string ThirdLinuxUserName = "thirduser";
        readonly static string ThirdLinuxUserPassword = Utilities.CreatePassword();
        readonly static string ThirdLinuxUserExpiration = "2020-12-31";

        readonly static string LinuxCustomScriptExtensionName = "CustomScriptForLinux";
        readonly static string LinuxCustomScriptExtensionPublisherName = "Microsoft.OSTCExtensions";
        readonly static string LinuxCustomScriptExtensionTypeName = "CustomScriptForLinux";
        readonly static string LinuxCustomScriptExtensionVersionName = "1.4";

        readonly static string MySqlScriptLinuxInstallCommand = "bash install_mysql_server_5.6.sh Abc.123x(";
        readonly static List<string> MySQLLinuxInstallScriptFileUris = new List<string>()
        {
            "https://raw.githubusercontent.com/melina5656/azure-quickstart-templates/master/application-workloads/mysql/mysql-standalone-server-ubuntu/scripts/install_mysql_server_5.6.sh"
        };

        readonly static string windowsCustomScriptExtensionName = "CustomScriptExtension";
        readonly static string windowsCustomScriptExtensionPublisherName = "Microsoft.Compute";
        readonly static string windowsCustomScriptExtensionTypeName = "CustomScriptExtension";
        readonly static string windowsCustomScriptExtensionVersionName = "1.7";

        readonly static string mySqlScriptWindowsInstallCommand = "powershell.exe -ExecutionPolicy Unrestricted -File installMySQL.ps1";
        readonly static List<string> mySQLWindowsInstallScriptFileUris = new List<string>()
        {
            "https://raw.githubusercontent.com/Azure/azure-libraries-for-net/master/Samples/Asset/installMySQL.ps1"
        };

        readonly static string linuxVmAccessExtensionName = "VMAccessForLinux";
        readonly static string linuxVmAccessExtensionPublisherName = "Microsoft.OSTCExtensions";
        readonly static string linuxVmAccessExtensionTypeName = "VMAccessForLinux";
        readonly static string linuxVmAccessExtensionVersionName = "1.4";

        // Windows configurations
        readonly static string firstWindowsUserName = Utilities.CreateUsername();
        readonly static string firstWindowsUserPassword = Utilities.CreatePassword();
        readonly static string firstWindowsUserNewPassword = Utilities.CreatePassword();

        readonly static string secondWindowsUserName = "seconduser";
        readonly static string secondWindowsUserPassword = Utilities.CreatePassword();

        readonly static string thirdWindowsUserName = "thirduser";
        readonly static string thirdWindowsUserPassword = Utilities.CreatePassword();

        readonly static string windowsVmAccessExtensionName = "VMAccessAgent";
        readonly static string windowsVmAccessExtensionPublisherName = "Microsoft.Compute";
        readonly static string windowsVmAccessExtensionTypeName = "VMAccessAgent";
        readonly static string windowsVmAccessExtensionVersionName = "2.3";
        
        /**
         * Azure Compute sample for managing virtual machine extensions. -
         *  - Create a Linux and Windows virtual machine
         *  - Add three users (user names and passwords for windows, SSH keys for Linux)
         *  - Resets user credentials
         *  - Remove a user
         *  - Install MySQL on Linux | something significant on Windows
         *  - Remove extensions
         */

        private static ResourceIdentifier? _resourceGroupId = null;
        public static async Task RunSample(ArmClient client)
        {
            /*
            string rgName = SdkContext.RandomResourceName("rgCOVE", 15);//完成
            string linuxVmName = SdkContext.RandomResourceName("lVM", 10);//完成
            string windowsVmName = SdkContext.RandomResourceName("wVM", 10);//完成
            string pipDnsLabelLinuxVM = SdkContext.RandomResourceName("rgPip1", 25);//完成
            string pipDnsLabelWindowsVM = SdkContext.RandomResourceName("rgPip2", 25);//完成
            */
            try
            {
                //=============================================================

                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
                var rgName = Utilities.CreateRandomName("rgCOVE");
                Utilities.Log($"creating a resource group with name : {rgName}...");
                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                var resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                // Create a linux virtual network
                Utilities.Log("Creating a Linux virtual network...");
                var virtualNetworkName = Utilities.CreateRandomName("VirtualNetwork_");
                var virtualNetworkCollection = resourceGroup.GetVirtualNetworks();
                var data = new VirtualNetworkData()
                {
                    Location = AzureLocation.EastUS,
                    AddressPrefixes =
                    {
                        new string("10.0.0.0/28"),
                    },
                };
                var virtualNetworkLro = await virtualNetworkCollection.CreateOrUpdateAsync(WaitUntil.Completed, virtualNetworkName, data);
                var virtualNetwork = virtualNetworkLro.Value;
                Utilities.Log("Created a Linux virtual network with name : " + virtualNetwork.Data.Name);

                // Create a public IP address
                Utilities.Log("Creating a Linux Public IP address...");
                var publicAddressIPCollection = resourceGroup.GetPublicIPAddresses();
                var publicIPAddressName = Utilities.CreateRandomName("pin");
                var pipDnsLabelLinuxVM = Utilities.CreateRandomName("rgpip1");
                var publicIPAddressdata = new PublicIPAddressData()
                {
                    Location = AzureLocation.EastUS,
                    Sku = new PublicIPAddressSku()
                    {
                        Name = PublicIPAddressSkuName.Standard,
                    },
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Static,
                    DnsSettings = new PublicIPAddressDnsSettings()
                    {
                        DomainNameLabel = pipDnsLabelLinuxVM
                    },
                };
                var publicIPAddressLro = await publicAddressIPCollection.CreateOrUpdateAsync(WaitUntil.Completed, publicIPAddressName, publicIPAddressdata);
                var publicIPAddress = publicIPAddressLro.Value;
                Utilities.Log("Creating a Linux Public IP address with name : " + publicIPAddress.Data.Name);

                //Create a subnet
                Utilities.Log("Creating a Linux subnet...");
                var subnetName = Utilities.CreateRandomName("subnet_");
                var subnetData = new SubnetData()
                {
                    ServiceEndpoints =
                    {
                        new ServiceEndpointProperties()
                        {
                            Service = "Microsoft.Storage"
                        }
                    },
                    Name = subnetName,
                    AddressPrefix = "10.0.0.0/28",
                };
                var subnetLRro = await virtualNetwork.GetSubnets().CreateOrUpdateAsync(WaitUntil.Completed, subnetName, subnetData);
                var subnet = subnetLRro.Value;
                Utilities.Log("Created a Linux subnet2 with name : " + subnet.Data.Name);

                //Create a networkInterface
                Utilities.Log("Created a linux networkInterface");
                var networkInterfaceData = new NetworkInterfaceData()
                {
                    Location = AzureLocation.EastUS,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "internal",
                            Primary = true,
                            Subnet = new SubnetData
                            {
                                Name = subnetName,
                                Id = new ResourceIdentifier($"{virtualNetwork.Data.Id}/subnets/{subnetName}")
                            },
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddress = publicIPAddress.Data,
                        }
                    }
                };
                var networkInterfaceName = Utilities.CreateRandomName("networkInterface");
                var nic = (await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, networkInterfaceName, networkInterfaceData)).Value;
                Utilities.Log("Created a Linux network interface with name : " + nic.Data.Name);

                //Create a VM with the Public IP address
                Utilities.Log("Creating a LinuxVM with the Public IP address...");
                var virtualMachineCollection = resourceGroup.GetVirtualMachines();
                var linuxVmName = Utilities.CreateRandomName("lVM");
                var linuxComputerName = Utilities.CreateRandomName("linuxComputer");
                var linuxVmdata = new VirtualMachineData(AzureLocation.EastUS)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = "Standard_D2a_v4"
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        AdminUsername = FirstLinuxUserName,
                        AdminPassword = FirstLinuxUserPassword,
                        ComputerName = linuxComputerName,
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic.Id,
                                Primary = true,
                            }
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                        {
                            OSType = SupportedOperatingSystemType.Linux,
                            Caching = CachingType.ReadWrite,
                            ManagedDisk = new VirtualMachineManagedDisk()
                            {
                                StorageAccountType = StorageAccountType.StandardLrs
                            }
                        },
                        ImageReference = new ImageReference()
                        {
                            Publisher = "Canonical",
                            Offer = "UbuntuServer",
                            Sku = "16.04-LTS",
                            Version = "latest",
                        }
                    },
                };
                var linuxVmLro = await virtualMachineCollection.CreateOrUpdateAsync(WaitUntil.Completed, linuxVmName, linuxVmdata);
                var linuxVM = linuxVmLro.Value;
                Utilities.Log($"Created the linuxVM with Id : " + linuxVM.Data.Id);
                
                //=============================================================
                
                // Add a second sudo user to Linux VM using VMAccess extension
                Utilities.Log("Creating virtualMachineExtension to the Linux VM...");
                var virtualMachineExtensionCollection = linuxVM.GetVirtualMachineExtensions();
                var linuxSettings = new
                {
                    username = SecondLinuxUserName,
                    password = SecondLinuxUserPassword,
                    expiration = SecondLinuxUserExpiration
                };
                var linuxBinaryData = BinaryData.FromObjectAsJson(linuxSettings);
                var linuxVmExtensionData = new VirtualMachineExtensionData(AzureLocation.EastUS)
                {
                    Publisher = linuxVmAccessExtensionPublisherName,
                    ExtensionType = linuxVmAccessExtensionTypeName,
                    TypeHandlerVersion = linuxVmAccessExtensionVersionName,
                    ProtectedSettings = linuxBinaryData,
                };
                var linuxVmExtension = (await virtualMachineExtensionCollection.CreateOrUpdateAsync(WaitUntil.Completed, linuxVmAccessExtensionName, linuxVmExtensionData)).Value;
                Utilities.Log("Created virtualMachineExtension to the Linux VM with name :" + linuxVmExtension.Data.Name);
                Utilities.Log("Added a second sudo user to the Linux VM");

                //=============================================================

                // Add a third sudo user to Linux VM by updating VMAccess extension
                Utilities.Log("Adding a third sudo user to the Linux VM...");
                var _settings = new
                {
                    username = ThirdLinuxUserName,
                    password = ThirdLinuxUserPassword,
                    expiration = ThirdLinuxUserExpiration
                };
                var _binaryData = BinaryData.FromObjectAsJson(_settings);
                var patch = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = _binaryData
                };
                _ = await linuxVmExtension.UpdateAsync(WaitUntil.Completed,patch);
                Utilities.Log("Added a third sudo user to the Linux VM");

                //=============================================================

                // Reset ssh password of first user of Linux VM by updating VMAccess extension
                var settings_ = new
                {
                    username = FirstLinuxUserName,
                    password = FirstLinuxUserNewPassword,
                    reset_ssh = true
                };
                var binaryData_ = BinaryData.FromObjectAsJson(settings_);
                var patch_ = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = binaryData_
                };
                _ = await linuxVmExtension.UpdateAsync(WaitUntil.Completed, patch_);
                Utilities.Log("Password of first user of Linux VM has been updated");

                //=============================================================

                // Removes the second sudo user from Linux VM using VMAccess extension
                Utilities.Log("Removing the second sudo user from Linux VM using VMAccess extension...");
                var _settings_ = new
                {
                    remove_user = SecondLinuxUserName,
                };
                var _binaryData_ = BinaryData.FromObjectAsJson(_settings_);
                var _patch_ = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = _binaryData_
                };
                _ = await linuxVmExtension.UpdateAsync(WaitUntil.Completed, _patch_);
                Utilities.Log("Removed the second sudo user from Linux VM using VMAccess extension");

                //=============================================================

                // Install MySQL in Linux VM using CustomScript extension
                Utilities.Log("Creating virtualMachineExtension2 to the Linux VM...");
                var linuxSettings2 = new
                {
                    fileUris = MySQLLinuxInstallScriptFileUris,
                    commandToExecute = MySqlScriptLinuxInstallCommand
                };
                var linuxBinaryData2 = BinaryData.FromObjectAsJson(linuxSettings2);
                var linuxVmExtensionData2 = new VirtualMachineExtensionData(AzureLocation.EastUS)
                {
                    Publisher = LinuxCustomScriptExtensionPublisherName,
                    ExtensionType = LinuxCustomScriptExtensionTypeName,
                    TypeHandlerVersion = LinuxCustomScriptExtensionVersionName,
                    AutoUpgradeMinorVersion = true,
                    Settings = linuxBinaryData2
                };
                var linuxVmExtension2 = (await virtualMachineExtensionCollection.CreateOrUpdateAsync(WaitUntil.Completed, LinuxCustomScriptExtensionName, linuxVmExtensionData2)).Value;
                Utilities.Log("Created virtualMachineExtension2 to the Linux VM with name :" + linuxVmExtension2.Data.Name);
                Utilities.Log("Installed MySql using custom script extension");
               
                //=============================================================

                // Removes the extensions from Linux VM
                await linuxVmExtension.DeleteAsync(WaitUntil.Completed);
                await linuxVmExtension2.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Removed the custom script and VM Access extensions from Linux VM");
                var count = linuxVM.GetVirtualMachineExtensions().ToList().Count;
                Utilities.Log(count);

                //=============================================================

                // Create a virtual network
                Utilities.Log("Creating a windows virtual network...");
                var windowsVirtualNetworkName = Utilities.CreateRandomName("VirtualNetwork");
                var windowsVirtualNetworkCollection = resourceGroup.GetVirtualNetworks();
                var windowsVirtualNetworkdata = new VirtualNetworkData()
                {
                    Location = AzureLocation.EastUS,
                    AddressPrefixes =
                    {
                        new string("10.0.0.0/28"),
                    },
                };
                var windowsVirtualNetworkLro = await windowsVirtualNetworkCollection.CreateOrUpdateAsync(WaitUntil.Completed, windowsVirtualNetworkName, windowsVirtualNetworkdata);
                var windowsVirtualNetwork = windowsVirtualNetworkLro.Value;
                Utilities.Log("Created a windows virtual network with name : " + windowsVirtualNetwork.Data.Name);

                // Create a public IP address
                Utilities.Log("Creating a windows Public IP address...");
                var windowsPublicAddressIPCollection = resourceGroup.GetPublicIPAddresses();
                var windowsPublicIPAddressName = Utilities.CreateRandomName("pin");
                var pipDnsLabelWindowsVM = Utilities.CreateRandomName("rgpip2");
                var windowsPublicIPAddressdata = new PublicIPAddressData()
                {
                    Location = AzureLocation.EastUS,
                    Sku = new PublicIPAddressSku()
                    {
                        Name = PublicIPAddressSkuName.Standard,
                    },
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Static,
                    DnsSettings = new PublicIPAddressDnsSettings()
                    {
                        DomainNameLabel = pipDnsLabelWindowsVM
                    },
                };
                var windowsPublicIPAddressLro = await windowsPublicAddressIPCollection.CreateOrUpdateAsync(WaitUntil.Completed, windowsPublicIPAddressName, windowsPublicIPAddressdata);
                var windowsPublicIPAddress = windowsPublicIPAddressLro.Value;
                Utilities.Log("Creating a windows Public IP address with name : " + windowsPublicIPAddress.Data.Name);

                //Create a subnet
                Utilities.Log("Creating a windows subnet...");
                var windowsSubnetName = Utilities.CreateRandomName("subnet_");
                var windowsSubnetData = new SubnetData()
                {
                    ServiceEndpoints =
                    {
                        new ServiceEndpointProperties()
                        {
                            Service = "Microsoft.Storage"
                        }
                    },
                    Name = windowsSubnetName,
                    AddressPrefix = "10.0.0.0/28",
                };
                var windowsSubnetLRro = await windowsVirtualNetwork.GetSubnets().CreateOrUpdateAsync(WaitUntil.Completed, windowsSubnetName, windowsSubnetData);
                var windowsSubnet = windowsSubnetLRro.Value;
                Utilities.Log("Created a windows subnet2 with name : " + windowsSubnet.Data.Name);

                //Create a networkInterface
                Utilities.Log("Created a windows networkInterface");
                var windowsNetworkInterfaceData = new NetworkInterfaceData()
                {
                    Location = AzureLocation.EastUS,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "internal",
                            Primary = true,
                            Subnet = new SubnetData
                            {
                                Name = windowsSubnetName,
                                Id = new ResourceIdentifier($"{windowsVirtualNetwork.Data.Id}/subnets/{windowsSubnetName}")
                            },
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddress = windowsPublicIPAddress.Data,
                        }
                    }
                };
                var windowsNetworkInterfaceName = Utilities.CreateRandomName("networkInterface");
                var nic_ = (await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, windowsNetworkInterfaceName, windowsNetworkInterfaceData)).Value;
                Utilities.Log("Created a windows network interface with name : " + nic.Data.Name);

                //Create a VM with the Public IP address
                Utilities.Log("Creating a windowsVM with the Public IP address...");
                var windowsVMName = Utilities.CreateRandomName("wVM");
                var windowsComputerName = Utilities.CreateRandomName("wComputer");
                var windowsVirtualMachineData = new VirtualMachineData(AzureLocation.EastUS)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = "Standard_D2a_v4"
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        AdminUsername = firstWindowsUserName,
                        AdminPassword = firstWindowsUserPassword,
                        ComputerName = windowsComputerName,
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic_.Id,
                                Primary = true,
                            }
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                        {
                            OSType = SupportedOperatingSystemType.Windows,
                            Caching = CachingType.ReadWrite,
                            ManagedDisk = new VirtualMachineManagedDisk()
                            {
                                StorageAccountType = StorageAccountType.StandardLrs
                            }
                        },
                        ImageReference = new ImageReference()
                        {
                            Publisher = "MicrosoftWindowsServer",
                            Offer = "WindowsServer",
                            Sku = "2012-R2-Datacenter",
                            Version = "latest",
                        }
                    },
                };
                var windowsVirtualMachineLro = await virtualMachineCollection.CreateOrUpdateAsync(WaitUntil.Completed, windowsVMName, windowsVirtualMachineData);
                var windowsVM = windowsVirtualMachineLro.Value;
                Utilities.Log($"Created the windowsVM with Id : " + windowsVM.Data.Id);

                //Create a windowsVmExtension
                Utilities.Log("Creating windowsVmExtension to the windows VM...");
                var windowsVMExtensionCollection = windowsVM.GetVirtualMachineExtensions();
                var windowsSettings = new
                {
                    fileUris = mySQLWindowsInstallScriptFileUris,
                    commandToExecute = mySqlScriptWindowsInstallCommand,
                };
                var windowsbinaryData = BinaryData.FromObjectAsJson(windowsSettings);
                var windowsExtensionData = new VirtualMachineExtensionData(AzureLocation.EastUS)
                {
                    Publisher = windowsCustomScriptExtensionPublisherName,
                    ExtensionType = windowsCustomScriptExtensionTypeName,
                    TypeHandlerVersion = windowsCustomScriptExtensionVersionName,
                    ProtectedSettings = windowsbinaryData,
                    AutoUpgradeMinorVersion = true
                };
                var windowsVMExtension = (await windowsVMExtensionCollection.CreateOrUpdateAsync(WaitUntil.Completed, windowsCustomScriptExtensionName, windowsExtensionData)).Value;
                Utilities.Log("Created windowsVmExtension to the windows VM with name :" + windowsVMExtension.Data.Name);

                //=============================================================

                // Add a second admin user to Windows VM using VMAccess extension
                Utilities.Log("Creating virtualMachineExtension to the windows VM...");
                var windowsSettings2 = new
                {
                    username = secondWindowsUserName,
                    password = secondWindowsUserPassword,
                };
                var windowsBinaryData2 = BinaryData.FromObjectAsJson(windowsSettings2);
                var windowsExtensionData2 = new VirtualMachineExtensionData(AzureLocation.EastUS)
                {
                    Publisher = windowsVmAccessExtensionPublisherName,
                    ExtensionType = windowsVmAccessExtensionTypeName,
                    TypeHandlerVersion = windowsVmAccessExtensionVersionName,
                    ProtectedSettings = windowsBinaryData2,
                    AutoUpgradeMinorVersion = true
                };
                var windowsVmAccessExtension = (await windowsVMExtensionCollection.CreateOrUpdateAsync(WaitUntil.Completed, windowsVmAccessExtensionName, windowsExtensionData2)).Value;
                Utilities.Log("Created virtualMachineExtension to the windows VM with name :" + windowsVmAccessExtension.Data.Name);

                //=============================================================

                // Add a third admin user to Windows VM by updating VMAccess extension
                Utilities.Log("Adding a third sudo user to the Linux VM...");
                var windowsSettings_ = new
                {
                    username = thirdWindowsUserName,
                    password = thirdWindowsUserPassword,
                };
                var windowsbinaryData_ = BinaryData.FromObjectAsJson(windowsSettings_);
                var windowspatch_ = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = windowsbinaryData_
                };
                _ = await windowsVmAccessExtension.UpdateAsync(WaitUntil.Completed, windowspatch_);
                Utilities.Log("Added a third sudo user to the Linux VM");

                //=============================================================

                // Reset admin password of first user of Windows VM by updating VMAccess extension
                Utilities.Log("Password of first user of Windows VM is updating");
                var _windowsSettings = new
                {
                    username = firstWindowsUserName,
                    password = firstWindowsUserNewPassword,
                };
                var _windowsbinaryData = BinaryData.FromObjectAsJson(_windowsSettings);
                var _windowspatch = new VirtualMachineExtensionPatch()
                {
                    ProtectedSettings = _windowsbinaryData
                };
                _ = await windowsVmAccessExtension.UpdateAsync(WaitUntil.Completed, _windowspatch);
                Utilities.Log("Password of first user of Windows VM has been updated");

                //=============================================================

                // Removes the extensions from Linux VM
                Utilities.Log("Removing the VM Access extensions from Windows VM");
                await windowsVmAccessExtension.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Removed the VM Access extensions from Windows VM");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }
        public static async Task Main(string[] args)
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);
                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}
