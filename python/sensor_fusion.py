import numpy as np

def compute_orientation(accel, mag):
    ax, ay, az = accel
    mx, my, mz = mag

    roll_rad  = np.arctan2(ay, az)
    pitch_rad = np.arctan2(-ax, np.sqrt(ay**2 + az**2))

    roll  = np.degrees(roll_rad)
    pitch = np.degrees(pitch_rad)

    mx2 = mx * np.cos(pitch_rad) + mz * np.sin(pitch_rad)
    my2 = mx * np.sin(roll_rad) * np.sin(pitch_rad) + my * np.cos(roll_rad) - mz * np.sin(roll_rad) * np.cos(pitch_rad)

    heading = np.degrees(np.arctan2(-my2, mx2))

    if heading < 0:
        heading += 360

    return {
        "heading": round(float(heading), 2),
        "pitch":   round(float(pitch), 2),
        "roll":    round(float(roll), 2)
    }

def complementary_filter(prev_angle, gyro_rate, accel_angle, dt=0.01):
    return 0.98 * (prev_angle + gyro_rate * dt) + 0.02 * accel_angle

if __name__ == "__main__":
    accel = [0.1, 0.2, 9.8]
    gyro  = [0.5, 0.3, 0.1]
    mag   = [30.0, 5.0, -40.0]

    result = compute_orientation(accel, mag)
    print(f"Initial → {result}")

    roll  = result["roll"]
    pitch = result["pitch"]

    for i in range(5):
        roll  = complementary_filter(roll,  gyro[0], np.degrees(np.arctan2(accel[1], accel[2])))
        pitch = complementary_filter(pitch, gyro[1], np.degrees(np.arctan2(-accel[0], np.sqrt(accel[1]**2 + accel[2]**2))))
        print(f"Step {i+1} → heading: {result['heading']}, pitch: {round(pitch,2)}, roll: {round(roll,2)}")