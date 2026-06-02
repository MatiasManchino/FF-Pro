from PIL import Image
import os
path = os.path.join('Assets','Art','Map','Resources','Map','Textures','mask-water-land.png')
img = Image.open(path).convert('L')
print('size', img.size)

def sample(lat, lon):
    u = (lon + 180.0) / 360.0
    v = 1.0 - (lat + 90.0) / 180.0
    x = min(max(int(u * img.width), 0), img.width-1)
    y = min(max(int(v * img.height), 0), img.height-1)
    return img.getpixel((x, y))

for lon in [-180, -90, 0, 90, 180]:
    print('lon', lon, 'lat0', sample(0, lon))
for lat in [90, 0, -90]:
    print('lat', lat, 'lon0', sample(lat, 0))
for lon in [-180, -90, 0, 90, 180]:
    g = lon + 180
    if g > 180: g -= 360
    print('game lon', g, 'real lon', lon, 'sample', sample(0, g))
