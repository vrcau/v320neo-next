import json
import math
import random
from pathlib import Path

SAMPLE_COUNT = 601
RATE = 60.0
random.seed(42)

times = [round(i / RATE, 6) for i in range(SAMPLE_COUNT)]
altitude = []
airspeed = []
vertical_speed = []
grounded = []
pilot_input = []
ecam = []

for t in times:
    climbing = t >= 2.0
    target_altitude = min(35000.0, (t - 2.0) * 3500.0) if climbing else 0.0
    altitude.append(round(target_altitude + random.uniform(-3.0, 3.0) if climbing else 0.0, 1))
    airspeed.append(round(250.0 + 15.0 * math.sin(t * 0.7) + random.uniform(-0.8, 0.8), 1))
    vertical_speed.append(round(3500.0 + random.uniform(-40.0, 40.0) if climbing else 0.0, 1))
    grounded.append(not climbing)
    pilot_input.append([
        round(math.sin(t * 1.2) * (0.0 if not climbing else 0.4), 4),
        round(math.cos(t * 0.9) * (0.0 if not climbing else 0.3), 4),
        0.0,
    ])
    if 5.0 <= t < 6.0:
        ecam.append("ENG 1 FAIL")
    elif t < 3.0:
        ecam.append("APU AVAIL")
    else:
        ecam.append(None)

data = {
    "schemaVersion": 1,
    "source": "AvionicsBusRecorder",
    "recordedAtUtc": "2026-10-29T00:00:00.0000000Z",
    "samplingMethod": "Update",
    "sampleRateHz": RATE,
    "sampleCount": SAMPLE_COUNT,
    "durationSeconds": times[-1],
    "overflowed": False,
    "times": times,
    "parameters": [
        {
            "key": "float:12",
            "type": "float",
            "id": 12,
            "name": "V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet",
            "values": altitude,
        },
        {
            "key": "float:13",
            "type": "float",
            "id": 13,
            "name": "V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed",
            "values": airspeed,
        },
        {
            "key": "float:14",
            "type": "float",
            "id": 14,
            "name": "V32NN_Frequent_ADIRS_ADR_1_VerticalSpeedFeetPerMinute",
            "values": vertical_speed,
        },
        {
            "key": "bool:3",
            "type": "bool",
            "id": 3,
            "name": "Sim_Frequent_Grounded",
            "values": grounded,
        },
        {
            "key": "vector3:0",
            "type": "vector3",
            "id": 0,
            "name": "V32NN_Frequent_FlightControl_LocalPilotInput",
            "components": 3,
            "values": pilot_input,
        },
        {
            "key": "string:0",
            "type": "string",
            "id": 0,
            "name": "V32NN_Infrequent_ECAM_ActiveMessage",
            "values": ecam,
        },
    ],
}

out = Path(__file__).with_name("sample-export.js")
out.write_text(
    "window.AVIONICS_BUS_SAMPLE = "
    + json.dumps(data, ensure_ascii=False, separators=(",", ":"))
    + ";\n",
    encoding="utf-8",
)
print(out)
