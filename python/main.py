from fastapi import FastAPI
from navigation import get_navigation
from sensor_fusion import compute_orientation
app = FastAPI()
accel   = [0.1, 0.2, 9.8]
gyro    = [0.5, 0.3, 0.1]
mag     = [30.0, 5.0, -40.0]
current = (33.6844, 73.0479)
target  = (33.6938, 73.0651)

@app.get("/nav")
def get_nav():
    orientation = compute_orientation(accel, mag)
    navigation  = get_navigation(current, target, orientation["heading"])
    return {**orientation, **navigation}