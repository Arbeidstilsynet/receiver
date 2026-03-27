# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## 1.2.2

### Fixed

- Throw exception when altinn file metadata is missing required fields, instead of falling back to a dummy value.

## 1.2.1

### Fixed

- fix: Install curl as last step in dockerfile to make internal healthcheck via curl available. This was unintentionally removed during refactoring.

## 1.2.0

### Changed

- chore: Applied new hexarch v3 (updated file structure and renamed all hexarch related terms we used)

## 1.1.3

### Removed

- Removed the adhoc function to set the main document. This is no longer necessary since the database migration is now complete.

## 1.1.2

### Changed

- Applied new file structure for this repository. All files which are required to build and run the receiver are now placed under `./src`. Also added documentation which can be found within the `./documentation` directory.

## 1.1.1

### Fixed

- Nothing changed from 1.1.0, just fixed a pipeline issue which caused the publish workflow to fail.

## 1.1.0

### Changed

- Differentiate between MainContent and StructuredData on Melding
- Add Tags to Documents
- Add GetMeldinger to IMeldingerClient
- Treat structured data and main content as clean when coming from Altinn (since they are both generated based on the DataModel of the instance)
- Consumers should assume a melding can be republished, and should update their internals if they get another melding with the same meldingId.

### Added

- Added adhoc function to set the main document, and re-publish the melding. This is necessary due to incomplete info in the db-layer during normal database migration.

## 1.0.0

### Added

- feat: possibility to configure altinn app related config via env variables

### Changed

- chore: moved all core `receiver` functionality from the meldinger repo to this repository. We will from now on start tracking changes, and deploy changes in this repository

## 0.X.X

### Fixed

- fix: reverted the way we handled valkey consumers since it created unnecessary overhead. With this change, each consumer is creating one valkey consumer group, and filters relevant messages based on its `AppRegistrations`.

### Added

- feat: added possibility to define multiple `AppRegistrations` per consumer. The contract which defines this is called `ConsumerManifest` and is to be defined by potential consumers / when using the ``POST /subscriptions`` endpoint. This makes it possible that one consumer can consume e.g. multiple altinn apps.
