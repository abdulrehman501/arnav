import numpy as np

def haversine(current, target):
    R = 6371000
    lat1, lon1 = np.radians(current)
    lat2, lon2 = np.radians(target)
    dlat = lat2 - lat1
    dlon = lon2 - lon1
    a = np.sin(dlat/2)**2 + np.cos(lat1) * np.cos(lat2) * np.sin(dlon/2)**2
    distance = 2 * R * np.arcsin(np.sqrt(a))
    return round(distance, 2)

def bearing(current, target):
    lat1, lon1 = np.radians(current)
    lat2, lon2 = np.radians(target)
    dlat = lat2 - lat1
    dlon = lon2 - lon1
    x = np.sin(dlon) * np.cos(lat2)
    y = np.cos(lat1) * np.sin(lat2) - np.sin(lat1) * np.cos(lat2) * np.cos(dlon)
    angle = np.degrees(np.arctan2(x, y))
    return (angle + 360) % 360

def turn_direction(current_heading, target_bearing):
    diff = target_bearing - current_heading
    if diff > 180: diff -= 360
    if diff < -180: diff += 360
    if diff > 10: return "right"
    if diff < -10: return "left"
    return "straight"

def get_navigation(current, target, current_heading):
    d = haversine(current, target)
    b = bearing(current, target)
    t = turn_direction(current_heading, b)
    return {
        "distance":  round(float(d), 2),
        "bearing":   round(float(b), 2),
        "direction": t
    }

if __name__ == "__main__":
    current = (33.6844, 73.0479)
    target  = (33.6938, 73.0651)
    heading = 9.46
    print(get_navigation(current, target, heading))