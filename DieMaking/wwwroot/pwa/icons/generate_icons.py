#!/usr/bin/env python3
import os

# 创建简单的 HTML 文件，可以在浏览器中打开并截图生成图标
html_content = '''<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>PWA Icon Generator</title>
    <style>
        body { margin: 0; background: #f0f0f0; font-family: Arial; }
        .container { display: flex; flex-wrap: wrap; gap: 20px; padding: 20px; }
        .icon-box { text-align: center; }
        .icon { background: #2196F3; border-radius: 15%; display: flex; align-items: center; justify-content: center; color: white; font-weight: bold; margin-bottom: 10px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="icon-box"><div class="icon" style="width:72px;height:72px;font-size:28px;">DM</div>72x72</div>
        <div class="icon-box"><div class="icon" style="width:96px;height:96px;font-size:36px;">DM</div>96x96</div>
        <div class="icon-box"><div class="icon" style="width:128px;height:128px;font-size:48px;">DM</div>128x128</div>
        <div class="icon-box"><div class="icon" style="width:144px;height:144px;font-size:54px;">DM</div>144x144</div>
        <div class="icon-box"><div class="icon" style="width:152px;height:152px;font-size:57px;">DM</div>152x152</div>
        <div class="icon-box"><div class="icon" style="width:192px;height:192px;font-size:72px;">DM</div>192x192</div>
        <div class="icon-box"><div class="icon" style="width:384px;height:384px;font-size:144px;">DM</div>384x384</div>
        <div class="icon-box"><div class="icon" style="width:512px;height:512px;font-size:192px;">DM</div>512x512</div>
    </div>
</body>
</html>'''

with open('icon_generator.html', 'w') as f:
    f.write(html_content)

print("Created icon_generator.html - open in browser to see icon previews")

# 创建占位 PNG 文件（使用 base64 编码的简单图标）
import base64

# 简单的 1x1 像素 PNG 数据（蓝色）
sizes = [72, 96, 128, 144, 152, 192, 384, 512]

# 创建一个简单的 ICO 文件说明
with open('README.txt', 'w') as f:
    f.write('''PWA Icons
=========

Required sizes: 72x72, 96x96, 128x128, 144x144, 152x152, 192x192, 384x384, 512x512

Source file: icon.svg

To generate PNG icons from SVG:
1. Use an online converter like https://cloudconvert.com/svg-to-png
2. Or use ImageMagick: for size in 72 96 128 144 152 192 384 512; do convert -background none icon.svg -resize ${size}x${size} icon-${size}x${size}.png; done
3. Or open icon_generator.html in a browser and take screenshots

Note: icon.svg is a scalable vector graphic that can be used directly by modern browsers.
''')

print("Created README.txt with icon generation instructions")
print(f"Icons directory: {os.getcwd()}")
print("Files created:")
for f in os.listdir('.'):
    print(f"  - {f}")
