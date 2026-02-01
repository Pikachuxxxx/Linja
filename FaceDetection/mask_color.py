import cv2
import numpy as np
import socket

# -------------------------------
# Load DNN face detector
# -------------------------------
net = cv2.dnn.readNetFromCaffe(
    "deploy.prototxt",
    "res10_300x300_ssd_iter_140000.caffemodel"
)

CONF_THRESHOLD = 0.6

# -------------------------------
# HSV color ranges
# -------------------------------
COLOR_RANGES = {
    "RED": [
        ((0,   60,  90), (8, 255, 255)),
        ((0,   45,  55), (12, 255, 200)),
        ((170, 45,  50), (179, 255, 180))
    ],
    # Blue includes cyan drift (covers cyan→blue)
    "BLUE": [
        ((80, 18, 40), (130, 255, 255))
    ],
    # Purple / lavender (light purple → deeper purple)
    "PURPLE": [
        ((115, 18, 140), (140, 255, 255)),
        ((110, 15,  90), (150, 255, 220)),
        ((105, 12,  80), (135, 200, 200)),
        ((120, 10,  65), (155, 180, 180))
    ],
    "YELLOW": [
        ((20,  80, 160), (35, 255, 255)),
        ((18,  60, 100), (40, 255, 200))
    ]
}


# -------------------------------
# Video loop
# -------------------------------
cap = cv2.VideoCapture(0)

def majority_color(roi_bgr):
    hsv = cv2.cvtColor(roi_bgr, cv2.COLOR_BGR2HSV)
    total_pixels = roi_bgr.shape[0] * roi_bgr.shape[1]

    counts = {}
    for color, ranges in COLOR_RANGES.items():
        c = 0
        for lo, hi in ranges:
            mask = cv2.inRange(hsv, np.array(lo), np.array(hi))
            c += cv2.countNonZero(mask)
        counts[color] = c

    best = max(counts, key=counts.get)
    best_ratio = counts[best] / total_pixels

    return best

UDP_IP = "127.0.0.1"   # Unity machine
UDP_PORT = 8008

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

COLOR_TO_ID = {
    None: 0,
    "RED": 1,
    "BLUE": 2,
    "PURPLE": 3,
    "YELLOW": 4
}

last_sent = None

def send_color(color):
    global last_sent
    color_id = COLOR_TO_ID[color]

    if color_id == last_sent:
        return

    sock.sendto(str(color_id).encode(), (UDP_IP, UDP_PORT))
    last_sent = color_id

while True:
    ret, frame = cap.read()
    if not ret:
        break

    h, w = frame.shape[:2]

    blob = cv2.dnn.blobFromImage(
        cv2.resize(frame, (300, 300)),
        1.0,
        (300, 300),
        (104.0, 177.0, 123.0)
    )

    net.setInput(blob)
    detections = net.forward()

    for i in range(detections.shape[2]):
        conf = detections[0, 0, i, 2]
        if conf < CONF_THRESHOLD:
            continue

        box = detections[0, 0, i, 3:7] * np.array([w, h, w, h])
        x1, y1, x2, y2 = box.astype(int)

        x1, y1 = max(0, x1), max(0, y1)
        x2, y2 = min(w, x2), min(h, y2)

        face = frame[y1:y2, x1:x2]
        if face.size == 0:
            continue

        fh = y2 - y1
        fw = x2 - x1

        roi_y1 = y1 + int(fh * 0.22)
        roi_y2 = y1 + int(fh * 0.62)
        roi_x1 = x1 + int(fw * 0.12)
        roi_x2 = x1 + int(fw * 0.88)

        mask_roi = frame[roi_y1:roi_y2, roi_x1:roi_x2]

        if mask_roi.size == 0:
            continue

        color = majority_color(mask_roi)
        label = color if color else "PURPLE"

        cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
        cv2.putText(
            frame,
            label,
            (x1, y1 - 8),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (0, 255, 0),
            2
        )
        
        send_color(label)

    cv2.imshow("Face → Mask Color", frame)

    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
cv2.destroyAllWindows()

