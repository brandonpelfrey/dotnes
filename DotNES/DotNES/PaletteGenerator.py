# Palette information taken from http://wiki.nesdev.com/w/index.php/PPU_palettes
D = "333,014,006,326,403,503,510,420,320,120,031,040,022,000,000,000,555,036,027,407,507,704,700,630,430,140,040,053,044,000,000,000,777,357,447,637,707,737,740,750,660,360,070,276,077,000,000,000,777,567,657,757,747,755,764,772,773,572,473,276,467,000,000,000".split(',')

threeBitTo256 = lambda x: (int(x)+1) * 32 - 1

def threeColorsToU32(color):
    bytes = [color[0], color[1], color[2], '7']
    bytes = map(threeBitTo256, bytes)
    return bytes

t = 0
for color in D:
    print '0x' + ''.join(map(lambda c : format(c, '02X'), threeColorsToU32(color))) + ',',
    t+=1
    if t%4 == 0: print ''