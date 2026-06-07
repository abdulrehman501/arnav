from fastapi import FastAPI
from navigation import get_navigation
from sensor_fusion import compute_orientation
from pydantic import BaseModel
app = FastAPI()
sensor_state = {
    "accel": [0.1, 0.2, 9.8],
    "mag": [30.0, 5.0, -40.0],
    "gps": [33.6844, 73.0479]
}
target  = (33.6938, 73.0651)
class SensorData(BaseModel):
    accel: list[float]
    mag: list[float]
    gps: list[float]

@app.get("/nav")
def get_nav():
    orientation = compute_orientation(sensor_state["accel"], sensor_state["mag"])
    navigation  = get_navigation(tuple(sensor_state["gps"]), target, orientation["heading"])
    return {**orientation, **navigation}
@app.post("/sensors")
def post_sensors(data: SensorData):
    global sensor_state
    sensor_state["accel"] = data.accel
    sensor_state["mag"]   = data.mag
    sensor_state["gps"]   = data.gps
    return {"status": "ok"}
