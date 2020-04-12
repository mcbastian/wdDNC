#!/usr/bin/python
import sys
from astropy.io import fits
import numpy as np
hdu = fits.open(sys.argv[1])
str = repr(hdu[0].header)
print(str, file=open(sys.argv[2], 'w'))
hdu.close()
