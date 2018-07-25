# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [v0.2.0] - 2018-07-25
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
