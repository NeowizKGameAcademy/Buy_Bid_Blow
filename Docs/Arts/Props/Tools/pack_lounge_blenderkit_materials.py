"""Pack downloaded BlenderKit PBR maps into the existing game UV layout.

No geometry changes. The printed artwork and fabric palette remain from v1.
Source IDs/licences live beside the unmodified source maps.
"""
from pathlib import Path
from PIL import Image, ImageOps
import numpy as np

ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'Docs/Arts/Props/MaterialSources/LoungeAccessories'
DST=ROOT/'Assets/Project/Content/Props/LoungeAccessories/Textures'
base=Image.open(DST/'Lounge_Atlas_BaseColor.png').convert('RGB')
mask=Image.open(DST/'Lounge_Atlas_MetallicSmoothness.png').convert('RGBA')
normal=Image.new('RGB',(1024,1024),(128,128,255))

def load(name,size,rotate=False,mode='RGB'):
    im=Image.open(SRC/(name+'.png')).convert(mode)
    if rotate:im=im.transpose(Image.Transpose.ROTATE_90)
    return im.resize(size,Image.Resampling.LANCZOS)

def pack_mask(metal,rough):
    zero=Image.new('L',rough.size,0)
    return Image.merge('RGBA',(metal,zero,zero,ImageOps.invert(rough)))

def normal_map(name,size,strength,rotate=False,flip_green=False):
    n=np.asarray(load(name,size,rotate),dtype=np.float32)/255*2-1
    if flip_green:n[:,:,1]*=-1
    if rotate:
        x=n[:,:,0].copy();n[:,:,0]=-n[:,:,1];n[:,:,1]=x
    n[:,:,:2]*=strength
    n[:,:,2]=np.sqrt(np.maximum(.001,1-n[:,:,0]**2-n[:,:,1]**2))
    return Image.fromarray(np.clip((n*.5+.5)*255,0,255).astype('uint8'))

wood_size=(512,1024)
base.paste(load('Polished Walnut Wood_BaseColor',wood_size,True),(0,0))
mask.paste(pack_mask(Image.new('L',wood_size,0),load('Polished Walnut Wood_Roughness',wood_size,True,'L')),(0,0))
normal.paste(normal_map('Polished Walnut Wood_Normal',wood_size,.35,rotate=True),(0,0))
metal_size=(256,256)
base.paste(load('metal_diffuse',metal_size),(512,0))
mask.paste(pack_mask(load('metal_metalness',metal_size,mode='L'),load('metal_roughness',metal_size,mode='L')),(512,0))
# BlenderKit brass source shader explicitly inverts its DirectX green channel.
normal.paste(normal_map('metal_normal',metal_size,.25,flip_green=True),(512,0))
base.save(DST/'Lounge_BK_Atlas_BaseColor.png')
mask.save(DST/'Lounge_BK_Atlas_MetallicSmoothness.png')
normal.save(DST/'Lounge_BK_Atlas_Normal.png')

# Preserve the source smudge pattern with a subtle real-time glass roughness range.
raw=np.asarray(load('metal-smudge-smoothness-4',(512,512),mode='L'),dtype=np.float32)/255
linear=np.where(raw<=.04045,raw/12.92,((raw+.055)/1.055)**2.4)
rough=Image.fromarray(np.round((.06+.18*linear)*255).astype('uint8'))
pack_mask(Image.new('L',rough.size,0),rough).save(DST/'Lounge_BK_Glass_MetallicSmoothness.png')
print('Packed BlenderKit walnut, brass, normal and glass roughness maps.')
