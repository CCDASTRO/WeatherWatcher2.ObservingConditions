"""Independent full-grid check using h5py/numpy; not a production dependency.

Usage: python verify-noaa-fixture.py ABI-L2-ACMF.nc [latitude longitude radius_km]
Equations: NOAA STAR / GOES-R PUG fixed-grid navigation. Scans the entire disk
in bounded row batches, independently checking the C# geographic bounding box.
"""
import sys
import h5py
import numpy as np

latitude, longitude, radius = map(float, sys.argv[2:5] or [35, -80, 15])
with h5py.File(sys.argv[1]) as f:
    def axis(name):
        d = f[name]
        return d[:].astype(float) * d.attrs["scale_factor"][0] + d.attrs["add_offset"][0]
    x, y = axis("x"), axis("y")
    p = f["goes_imager_projection"].attrs
    req, rpol = p["semi_major_axis"][0], p["semi_minor_axis"][0]
    h = p["perspective_point_height"][0] + req
    origin = np.deg2rad(p["longitude_of_projection_origin"][0])
    total = good = cloudy = 0
    for start in range(0, len(y), 128):
        xx, yy = np.meshgrid(x, y[start:start+128])
        a = np.sin(xx)**2 + np.cos(xx)**2 * (np.cos(yy)**2 + req**2/rpol**2 * np.sin(yy)**2)
        b = -2*h*np.cos(xx)*np.cos(yy)
        with np.errstate(invalid="ignore"):
            rs = (-b - np.sqrt(b*b - 4*a*(h*h-req*req))) / (2*a)
        sx, sy, sz = rs*np.cos(xx)*np.cos(yy), -rs*np.sin(xx), rs*np.cos(xx)*np.sin(yy)
        lat = np.arctan(req**2/rpol**2 * sz / np.sqrt((h-sx)**2+sy*sy))
        lon = origin - np.arctan2(sy, h-sx)
        hav = np.sin((lat-np.deg2rad(latitude))/2)**2 + np.cos(lat)*np.cos(np.deg2rad(latitude))*np.sin((lon-np.deg2rad(longitude))/2)**2
        inside = 2*6371.0088*np.arcsin(np.sqrt(np.minimum(1,hav))) <= radius
        bcm, dqf = f["BCM"][start:start+128], f["DQF"][start:start+128]
        valid = inside & (dqf == 0) & (bcm <= 1)
        total += np.count_nonzero(inside)
        good += np.count_nonzero(valid)
        cloudy += np.count_nonzero(valid & (bcm == 1))
    print(f"good={good}, total={total}, cloudy={cloudy}, CloudCover={100*cloudy/good:.12g}%")
