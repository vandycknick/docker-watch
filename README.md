# Docker Watch

[![AppVeyor build status][appveyor-badge]](https://ci.appveyor.com/project/nickvandyck/docker-watch/branch/master)

[appveyor-badge]: https://img.shields.io/appveyor/ci/nickvandyck/docker-watch/master.svg?label=appveyor&style=flat-square

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/docker-watch/
[main-nuget-badge]: https://img.shields.io/nuget/v/docker-watch.svg?style=flat-square&label=nuget


The idea of this tool is to provide a workaround for file change events in `docker for windows` not being fired inside mounted container volumes.
This is caused due to limitations of the current CIFS implementation in the Linux kernel. Which breaks watch modes that many frameworks, tools use to
do recompilation of an application during development (e.g. dotnet-watch, nodemon, webpack, ...). A common solution to this problem is to turn on polling
to make those tools pickup file changes. But that requires you to change your dockerfile's, development scripts, ... just to be able to develop on Windows
This is where this tools comes in to help you keep you scripts clean and use the same development process on Windows as you would on Mac or Linux.

## Installation

Download the [2.1.300](https://www.microsoft.com/net/download/windows) .NET Core SDK or newer.
Once installed, running the folling command:

```sh
dotnet tool install --global docker-watch
```

Or use the following when upgrading from a previous version:

```sh
dotnet tool update --global docker-watch
```

It requires `sh`, `stat` and `chmod` to already be installed inside a container. This
should be the case for most linux containers e.g. (ubuntu, debian, alpine, ...).

## Usage
Running `docker-watch` will setup monitoring for each mount in the current running containers.
It also sets up listeners for container start and stop events so that monitors will be kept in sync.

```sh
docker-watch
```

```
Notify docker containers about changes in mounted volumes.

Usage: docker-watch [options]

Options:
  --version                   Show version information
  -?|-h|--help                Show help information
  -v|--verbose                Enable more verbose and rich logging.
  -c|--container <CONTAINER>  Glob pattern to filter containers. Without providing a pattern, notifiers will get attached to each running container.
```
