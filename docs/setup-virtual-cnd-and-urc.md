# How to setup Virtual-CNS and UdonRadioCommunication-Redux

## VPM Repository and package required

- `https://vpm.virtualaviation.jp/vpm.json`
- `https://orels1.github.io/UdonToolkit/index.json`

After add repository listed above, please install following VPM packages.

- Udon Toolkit `sh.orels.udontoolkit`
- Udon Toolkit Inspector `sh.orels.udontoolkit.inspector`
- Udon Radio Communications Redux `jp.virtualaviation.urc-redux`
- Udon Radio Communications Redux Saccflight Addon `jp.virtualaviation.urc-redux-sf`

## Git package required

> [!IMPORTANT]
> Please install following given order.

> [!CAUTION]
> Please install all VPM packages required first before you continue. (see above)

1. `git+https://github.com/esnya/InariUdon.git?path=/Packages/com.nekometer.esnya.inari-udon`
2. `git+https://github.com/esnya/UdonRadioCommunications.git?path=/Packages/com.nekometer.esnya.udon-radio-communications` (We won't use this, it's required by Virtual-CNS)
3. `git+https://github.com/VirtualAviationJapan/Virtual-CNS.git?path=/Packages/jp.virtualaviation.virtual-cns`

## Setup scene

1. Search `UdonRadioCommunication` (Don't confuse with old URC prefab) and `NavaidDatabase` prefab.
2. Add them into scene.
3. Done.
