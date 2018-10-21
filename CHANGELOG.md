# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [v0.4.0]
### Added
- Ignores synchronizing file changes for git ignored files
- Added first round of unit tests

### Fixed
- Not synchronizing changes for single file mounts

### Changed
- Moved ci to azure pipelines

## [v0.3.0] - 2018-08-31
### Added
- New parameter (`--container`) that accepts a glob to filter the running containers for which you would like to watch for changes

### Fixed
- Monitor throws an error for volumes that only have a mount point in the container, but are not mounted in windows.

## [v0.2.0] - 2018-07-26
### Added
- improved logging
- added switch to turn on verbose logging
- added help information

### Changed
- Use docker events api to sync container volume notifiers
- Better management of executing a process in a container

## [v0.1.0] - 2018-07-22
### Added
- Watch for changes inside docker volumes
- Sync when watchers when containers get killed or created
