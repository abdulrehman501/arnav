## Integration point btw backend and unity app

# Endpoint

 endpoint-->/nav 
 method-->get  
 URL--->[here ngrok url]/nav 
 header-->ngrok-skip-browser-warning: true

# Schema 
| Heading       | Type   | Units   | Range               | Description                            |
| ------------- | ------ | ------- | ------------------- | -------------------------------------- |
| **heading**   | float  | degrees | 0–360               | Compass direction the device is facing |
| **pitch**     | float  | degrees | -90–90              | Forward/backward tilt of the device    |
| **roll**      | float  | degrees | -180–180            | Left/right tilt of the device          |
| **distance**  | float  | metres  | 0–∞                 | Straight-line distance to target       |
| **bearing**   | float  | degrees | 0–360               | Angle from North to target             |
| **direction** | string | —       | left/right/straight | Turn instruction for the user          |

# Sample Response 
 {
  "heading": 9.46,
  "pitch": -0.58,
  "roll": 1.17,
  "distance": 1903.93,
  "bearing": 56.7,
  "direction": "right"
}
