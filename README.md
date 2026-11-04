# V320neo-next

v320neo-next is an overhaul project based on the original [VAU320][old-vau320-github].

Its goal is to rework the aircraft systems, avionics, cockpit interactions, and input system, while also equipping the VAU320 with the ability to operate concurrently across different environments—whether running as a standalone aircraft, within the [SaccFlight][saccflight-github], or under a floating-origin system like [FDMi][fdmi-github].

Roadmap are still work-in-progress at this time.

## Progress

Currently v320neo-next fix serval issues that prevent original [VAU320][old-vau320-github] from operational. And improve the installation experience by simplify setup procedure and reduce the numbers of dependence.

## Differences from the original [VAU320][old-vau320-github]

### Changes

- Upgrade to SaccFlight v1.8.1
- Out-of-box experience, setup aircraft with just few clicks.
  - Drag `VAU320GlobalAircraftSettings` and aircraft prefab to scene
  - Click `Auto Setup` and select VHF voice protocol from UdonRadioCommunication
  - You are good to go! But you still need to setup `UdonRadioCommunication-Redux` and `NavaidDatabase` first.
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
- Functional flight director and auto pilot, just like real A320 do.
- New `LocalAircraftSettings` and `GlobalAircraftSetting` help you setup aircraft and change settings.
- Instrument rendering in static camera to reduce instrument glitch when far away from world origin.
- Functional ND map display. (map will move and rotation as aircraft moving, instead of stop at world origin).
- Workaround for camera position shifting in desktop mode.
- Avionics bus parameter recorder (`AvionicsBusRecorder`) with editor inspector, in-game JSON field / Debug.Log output, and a standalone web viewer for charts.

### Fixed

- Performance issue during A/THR activated.
- Gust wind won't show in ND wind display.
- Speed trend in PFD show inflated readings.
- U# asset of `FWSWanringData` will change every compile.

## Avionics data export

`AvionicsBusRecorder` can capture selected AvionicsBus parameters to JSON. The aircraft prefab already contains an idle recorder; configure it from the Inspector, or drive it from another UdonSharp script. A standalone web viewer is included under [tools/avionics-bus-export-viewer/](tools/avionics-bus-export-viewer/README.md).

See [docs/avionics-bus-recorder.md](docs/avionics-bus-recorder.md) for usage, the runtime API, and the JSON schema.

## Install

> [!IMPORTANT]
> v320neo-next are incompatible with [original VAU320][old-vau320-github].

> [!IMPORTANT]
> No VPM Repository provide as this time, you need to manual install dependencies.

### Dependencies

- Setup [Virtual-CNS][virtual-cns-github] and [UdonRadioCommunication-Redux][urc-redux-github] first
  - see [setup document here](docs/setup-virtual-cnd-and-urc.md)
- Install latest `Flight Menu` and `Flight Menu - Udon Radio Communications Redux Integration` from [our vpm repository](vpm-repo-home).

### Install aircraft package

1. Clone or download this repository
2. Copy `src/Packages/org.vrcau.vpm.aircrafts.v320neo-next` to the `Packages` folder of your project, or use `Add package from disk...` in Unity Package Manager.
3. Download match assets package from [our assets packages vpm repository][assets-vpm-repo-home].
   1. You can find match assets package version in `src/Packages/org.vrcau.vpm.aircrafts.v320neo-next\package.json`.
   2. Assets package version use [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### Install aircraft in your scene

> [!IMPORTANT]
> Please setup [UdonRadioCommunication-Redux][urc-redux-github] and NavaidDatabse from [Virtual-CNS][virtual-cns-github] first.
> For details, see [setup document here](docs/setup-virtual-cnd-and-urc.md).

> [!CAUTION]
> `UdonRadioCommunication-Redux` are incompatible with `UdonRadioCommunication`, DON'T use them at the same time in your scene.

1. Add `Aircraft/Prefab/VAU320GlobalAircraftSettings.prefab` to your scene, and click `Auto Setup Fields and Layers` first.
2. Fill up the **empty** field in `VAU320GlobalAircraftSettings`.
   1. `NavaidDatabase` - NavaidDatabase prefab in your scene, it should be name as `NavaidDatabase`. DON'T CHANGE IT'S NAME.
   2. `VoiceProtocolForVhf` - The `VoiceBroadcastByChannel` instance you use for VHF communication. It should be provide with `UdonRadioCommunication` prefab in [URC-Redux][urc-redux-github] samples. (Don't confuse with the prefab from [original URC][urc-original-github])
3. Add the aircraft prefab `Aircraft/Prefab/v320neo-next.prefab` into your scene.
4. Done.

## Contribute

Currently we don't accept contribution now.
If you want to join the development of aircraft, please contact us use following ways:

- QQ Group: [`526014547`](https://jq.qq.com/?_wv=1027&k=oH8yHGNS)
- Email: [`lipww1234@foxmail.com`](mailto:lipww1234@foxmail.com)

[old-vau320-github]: https://github.com/vrcau/VAU320/
[fdmi-github]: https://github.com/gyokute/FDMi
[saccflight-github]: https://github.com/Sacchan-VRC/SaccFlightAndVehicles
[esnya-sf-addons-github]: https://github.com/esnya/EsnyaSFAddons
[urc-redux-github]: https://github.com/VirtualAviationJapan/UdonRadioCommunications-Redux
[urc-original-github]: https://github.com/esnya/UdonRadioCommunications
[virtual-cns-github]: https://github.com/VirtualAviationJapan/Virtual-CNS
[assets-vpm-repo-home]: https://vpm.vrcau.org/public-assets/
[vpm-repo-home]: https://vpm.vrcau.org/public/
