# Welcome to Simple Networking

![Azure DevOps builds](https://img.shields.io/azure-devops/build/lalitadithya/3f21e28d-6bec-4451-adb0-3d3d4517ecc8/1)
![Azure DevOps tests](https://img.shields.io/azure-devops/tests/lalitadithya/simplenetworking/1)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/lalitadithya/simplenetworking/1)
![GitHub](https://img.shields.io/github/license/lalitadithya/simplenetworking)
![Nuget](https://img.shields.io/nuget/v/simplenetworking)
[![Documentation Status](https://readthedocs.org/projects/simplenetworking/badge/?version=latest)](https://simplenetworking.readthedocs.io/en/latest/?badge=latest)

Simple networking an easy to use networking library for .NET Core that guarantees exactly-once, in-order delivery of .NET objects while surviving network partitions. The library includes support for TLS 1.2, TLS 1.3 and mutual TLS 1.2/1.3. If you like SimpleNetworking, please consider starring the repository and spreading the word about SimpleNetworking. 

## Installing

SimpleNetworking is available on [nuget](https://www.nuget.org/packages/SimpleNetworking/) and can be installed by running the following command in this Package Manager Console within Visual Studio

```powershell
Install-Package SimpleNetworking -Version 0.1.1
```

Alternatively if you're using .NET Core then you can install SimpleNetworking via the command line interface with the following command:

```powershell
dotnet add package SimpleNetworking --version 0.1.1
```

## Getting started

You can find samples on how to use SimpleNetworking [here](GettingStarted.md)

## Contributing
I am happy to receive Pull Requests for adding new features and/or solving bugs. If you are facing a problem using SimpleNetworking, feel free to write up an issue [here](https://github.com/lalitadithya/SimpleNetworking/issues/new). 

## License

SimpleNetworking is available under [MIT License](https://github.com/lalitadithya/SimpleNetworking/blob/master/LICENSE)
