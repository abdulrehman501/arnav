from fastapi import FastAPI,UploadFile, File
from navigation import get_navigation
from sensor_fusion import compute_orientation
from pydantic import BaseModel
from depth import load_midas, estimate_depth
import shutil, os
app = FastAPI()
sensor_state = {
    "accel": [0.1, 0.2, 9.8],
    "mag": [30.0, 5.0, -40.0],
    "gps": [33.6844, 73.0479],
    "depth": 0.0
}
target = (33.7184, 73.0720)
model, transform = load_midas()
class SensorData(BaseModel):
    accel: list[float]
    mag: list[float]
    gps: list[float]
# user destination
class TargetData(BaseModel):
    lat: float
    lon: float

@app.get("/nav")
def get_nav():
    orientation = compute_orientation(sensor_state["accel"], sensor_state["mag"])
    navigation  = get_navigation(tuple(sensor_state["gps"]), target, orientation["heading"])
    return {**orientation, **navigation,"depth": sensor_state["depth"],"obstacle": sensor_state["depth"] < 2.0}
@app.post("/sensors")
def post_sensors(data: SensorData):
    global sensor_state
    sensor_state["accel"] = data.accel
    sensor_state["mag"]   = data.mag
    sensor_state["gps"]   = data.gps
    return {"status": "ok"}

@app.post("/frame")
async def post_frame(file: UploadFile = File(...)):
    temp_path = "temp_frame.jpg"
    with open(temp_path, "wb") as f:
        shutil.copyfileobj(file.file, f)
    depth=estimate_depth(temp_path,model,transform)
    os.remove(temp_path)
    sensor_state["depth"] = depth 
    return {"depth": depth}

@app.post("/target")
def post_target(data: TargetData):
    global target
    target = (data.lat, data.lon)
    return {"status": "ok"}
    



  