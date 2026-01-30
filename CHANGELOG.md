# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added <!-- for new features. -->

### Changed <!--  for changes in existing functionality. -->

### Deprecated <!--  for soon-to-be removed features. -->

### Removed <!-- for now removed features. -->

### Fixed <!-- for any bug fixes. -->

### Security <!-- in case of vulnerabilities. -->

## 3.0.2

### Added

- feat: possibility to configure altinn app related config via env variables

### Changed

- chore: first test deploy to ghcr image repository (should be published as receiver repo packages)

## 3.0.1

### Fixed

- fix: reverted the way we handled valkey consumers since it created unnecessary overhead. With this change, each consumer is creating one valkey consumer group, and filters relevant messages based on its `AppRegistrations`.

## 3.0.0

### Added

- feat: added possibility to define multiple `AppRegistrations` per consumer. The contract which defines this is called `ConsumerManifest` and is to be defined by potential consumers / when using the ``POST /subscriptions`` endpoint. This makes it possible that one consumer can consume e.g. multiple altinn apps.

### Changed

- chore: moved all core `receiver` functionality from the meldinger repo to this repository. We will from now on start tracking changes, and deploy changes in this repository.
