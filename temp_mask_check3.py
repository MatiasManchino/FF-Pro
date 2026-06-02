from PIL import Image
import os
path = os.path.join('Assets','Art','Map','Resources','Map','Textures','mask-water-land.png')
img = Image.open(path).convert('L')

def sample(lat, lon):
    u = (lon + 180) / 360
    v = 1 - (lat + 90) / 180
    x = min(max(int(u * img.width), 0), img.width - 1)
    y = min(max(int(v * img.height), 0), img.height - 1)
    return img.getpixel((x, y))

def game_lon(lon_real):
    g = lon_real + 180
    if g > 180:
        g -= 360
    return g

points = [
    ('London', 51.5, 0),
    ('New York', 40.7, -74),
    ('Miami', 25.77, -80),
    ('Barcelona', 41.4, 2.2),
    ('Dubai', 25.2, 55.3),
    ('Cairo', 30.0, 31.2),
    ('Panama', 9.0, -79.9),
    ('Suez', 30.0, 32.3),
    ('CapeTown', -33.9, 18.4),
    ('Tokyo', 35.7, 139.7)
]
print('name lat lon mask_real mask_game')
for name, lat, lon in points:
    gl = game_lon(lon)
    print(name, lat, lon, sample(lat, lon), sample(lat, gl))
