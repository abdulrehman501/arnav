import torch
import cv2
import numpy as np

def load_midas():
    model = torch.hub.load("intel-isl/MiDaS", "MiDaS_small")
    model.eval()
    transforms = torch.hub.load("intel-isl/MiDaS", "transforms")
    transform = transforms.small_transform
    return model, transform
    

def estimate_depth(image_path, model, transform):
    image = cv2.imread(image_path)
    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    input_tensor = transform(image)
    with torch.no_grad():
        prediction = model(input_tensor)
    return prediction.mean().item()
