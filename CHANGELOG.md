# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.1] - 2023-04-11
### Added
- Initial release

## [0.0.2] - 2023-04-12
### Changed
- Change Window Names
- Change MenuItem Priority

## [0.0.3] - 2023-04-13
### Changed
- Background Logs are now WarningLogs instead of ErrorLogs

## [0.0.4] - 2023-04-28
### Added
- Add Method to add a new LocaEntry

## [0.0.5] - 2023-04-28
### Changed
- database merge between online and local, now prioritize the online structure

## [0.0.6] - 2023-04-28
### Add
- add refresh, settings and export button to loca manager

## [0.0.7] - 2023-05-08
### Changed
- set a fixed minimum window size

## [0.0.8] - 2023-05-17
### Added
- Functionality to add and remove LocaEntries

## [0.0.9] - 2023-05-17
### Added
- search window

## [0.1.0] - 2023-05-24
### Added
- functionality to filter the shown LocaEntries

## [0.1.1] - 2023-05-25
### Added
- option to filter for uncomplete entries

## [0.1.2] - 2023-05-30
### Added
- add custom markups to the context menu of textfields
- custom markups can now be configured inside the settings

## [0.1.3] - 2023-05-30
### Added
- add public method to retrieve a LocaEntry by its key

## [0.1.4] - 2023-06-02
### Added
- extend locasearch to search by hash

## [0.1.5] - 2023-06-05
### Added
- add "hash <-> key" storage

## [0.1.6] - 2023-06-06
### Added
- add language option to the filter for uncomplete entries
### Changed
- labels are now full sized for a proper click event

## [0.1.7] - 2023-06-07
### Added
- add method to open the locamanager at a given position / key
- add method to create a new locaentry by key
- add method to rename a locaentry

## [0.1.8] - 2023-08-28
### Fixed
- filter empty keys on export

## [0.1.9] - 2023-09-04
### Added
- filteroption now filters content too

## [0.2.0] - 2023-09-11
### Fixed
- fixed Add Entry cancel behavior

## [0.2.1] - 2023-09-20
### Changed
- Add LocaEntry Window now opens in the center with focus on the input field

## [0.2.2] - 2023-09-26
### Added
- Markups now highligthed in if the richtext mode is enabled
- Show changes in the close dialog
### Fixed
- Fixed wrong paste behavior

## [0.2.3] - 2023-09-26
### Added
- support for readonly spreadsheets (google api key is required)
### Fixed
- Fixed wrong paste behavior (fixed a bug from the fix before)

## [0.2.4] - 2023-10-19
### Added
- background worker are now disabled for unity batchmode

## [0.2.5] - 2023-11-01
### Added
- new locaentry can now be confirmed via return button

## [0.2.6] - 2023-11-23
### Changed
- switch to newer version of our settingsprovider

## [0.2.7] - 2024-02-27
### Changed
- correctly fallback to revision check and catch exception if we got connection issues

## [0.2.8] - 2024-04-05
### Added
- add MenuEntry to Export Loc to JSON

## [0.2.9] - 2024-04-16
### Changed
- changed Drive Scope from DriveMetadataReadonly to DriveReadonly


## [0.3.0] - 2024-06-03
### Added
- add option to ignore defined languages for export

## [0.3.1] - 2024-06-03
### Added
- add loca entry validation on export
- add adapter

## [0.3.2] - 2024-10-08
### Changed
- text edits are now finished after focus lost to prevent HasChanges flag only got changes if there is one
