PWA Icons
=========

Required sizes: 72x72, 96x96, 128x128, 144x144, 152x152, 192x192, 384x384, 512x512

Source file: icon.svg

To generate PNG icons from SVG:
1. Use an online converter like https://cloudconvert.com/svg-to-png
2. Or use ImageMagick: for size in 72 96 128 144 152 192 384 512; do convert -background none icon.svg -resize ${size}x${size} icon-${size}x${size}.png; done
3. Or open icon_generator.html in a browser and take screenshots

Note: icon.svg is a scalable vector graphic that can be used directly by modern browsers.
