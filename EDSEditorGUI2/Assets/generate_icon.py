from PIL import Image, ImageDraw, ImageFont
import os

def create_icon():
    size = 256
    img = Image.new('RGBA', (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(img)
    
    bg_color = (41, 50, 65) # Dark slate
    accent_color = (238, 108, 77) # Accent orange
    
    # Draw rounded rectangle
    try:
        draw.rounded_rectangle([(16, 16), (240, 240)], radius=40, fill=bg_color)
    except AttributeError:
        draw.rectangle([(16, 16), (240, 240)], fill=bg_color)
    
    # Draw text
    try:
        font = ImageFont.truetype("arialbd.ttf", 100)
    except:
        font = ImageFont.load_default()
    
    text_c = "C"
    text_e = "E"
    
    try:
        # Get bounding box for full text to center it perfectly
        bbox_ce = draw.textbbox((0, 0), "CE", font=font)
        
        # Calculate exactly where to draw so the visual center matches the image center
        # The visual center of the text when drawn at (0,0) is:
        # cx = (bbox_ce[0] + bbox_ce[2]) / 2
        # cy = (bbox_ce[1] + bbox_ce[3]) / 2
        # We want the visual center to be at size / 2:
        x = (size / 2) - ((bbox_ce[0] + bbox_ce[2]) / 2)
        y = (size / 2) - ((bbox_ce[1] + bbox_ce[3]) / 2)
        
        # Get exact advance width of "C" to know where to place "E"
        w_c = draw.textlength("C", font=font)
    except AttributeError:
        # Fallback for older Pillow
        w, h = draw.textsize("CE", font=font)
        w_c, _ = draw.textsize("C", font=font)
        x = (size - w) / 2
        y = (size - h) / 2
    
    # Draw "C" in white
    draw.text((x, y), text_c, fill=(255, 255, 255), font=font)
    
    # Draw "E" in orange (accent_color)
    # We use w_c for the X offset.
    draw.text((x + w_c, y), text_e, fill=accent_color, font=font)
    
    out_path = r"c:\Users\14588\workspace\CANopenEditor\EDSEditorGUI2\Assets\icon.ico"
    icon_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    img.save(out_path, format="ICO", sizes=icon_sizes)
    print("Icon generated successfully.")

if __name__ == "__main__":
    create_icon()
