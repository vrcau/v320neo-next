# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changes

- Upgrade to SaccFlight v1.8.1
- Out-of-box experience, setup aircraft with just few clicks.
  - Drag `VAU320GlobalAircraftSettings` and aircraft prefab to scene
  - Click `Auto Setup` and select VHF voice protocol from UdonRadioCommunication
  - You are good to go! But you still need to setup `UdonRadioCommunication-Redux` and `NavaidDatabase` first.
- New engine sounds.
- Use Udon Radio Communications Redux. (Active maintenance and improved version of original URC)
- Adjust the SaccFlight DFUNC position.
- Assets like models, sounds and texture are moved into separated package.
  - Upgrade aircraft system won't require re-download all assets.
  - No Git LFS required. (Save budge for us and avoid trouble of manage Git LFS objects in Github)

### Added

- Migrate whole sacc dial to [flight-menu](https://github.com/vrcau/flight-menu) system.
  - Menu will show as overlay in desktop mode, and follow you hand in VR.
  - You can change VHF RX/TX and frequency using a input menu.
  - See how many trim you have apply, and change trim by push/pull thumbtack in trim menu.
  - Choice auto brake arm mode from menu.
  - Set almost everything using thumbtack instead of hold trigger and move your controller.
- Stall warning speed calculation.
- Functional flight director and auto pilot, just like real A320 do.
- New `LocalAircraftSettings` and `GlobalAircraftSetting` help you setup aircraft and change settings.
- Instrument rendering in static camera to reduce instrument glitch when far away from world origin.
- Functional ND map display. (map will move and rotation as aircraft moving, instead of stop at world origin).
- Workaround for camera position shifting in desktop mode.
- Avionics bus parameter recorder (`AvionicsBusRecorder`) with editor inspector, in-game JSON field / Debug.Log output, and a standalone web viewer for charts. See [docs/avionics-bus-recorder.md](docs/avionics-bus-recorder.md).

### Fixed

- Try select flap up in desktop mode when flap already up will crash flap controller.
- Takeoff with config 2 will trigger config warning.
- Performance issue during A/THR activated.
- Gust wind won't show in ND wind display.
- Speed trend in PFD show inflated readings.
- U# asset of `FWSWanringData` will change every compile.

[unreleased]: https://github.com/vrcau/v320neo-next/
