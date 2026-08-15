# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changes

- Upgrade to SaccFlight v1.8.1
- Use Udon Radio Communications Redux. (Active maintenance and improved version of original URC)
- Adjust the SaccFlight DFUNC position.
- Assets like models, sounds and texture are moved into separated package.
  - Upgrade aircraft system won't require re-download all assets.
  - No Git LFS required. (Save budge for us and avoid trouble of manage Git LFS objects in Github)

### Added

- Instrument rendering in static camera to reduce instrument glitch when far away from world origin.
- Functional ND map display. (map will move and rotation as aircraft moving, instead of stop at world origin).
- Workaround for camera position shifting in desktop mode.
- Sacc flight dial show as overlay for desktop mode, and following player hands (just like Action Menu) in VR.

### Fixed

- Fully functional boarding collider from EsnyaSFAddons which can make player follow the plane as it moves.
  - The boarding collider in original VAU320 only have a collider.
- Performance issue during A/THR activated.
- Gust wind won't show in ND wind display.
- Speed trend in PFD show inflated readings.
- U# asset of `FWSWanringData` will change every compile.

[unreleased]: https://github.com/vrcau/v320neo-next/
