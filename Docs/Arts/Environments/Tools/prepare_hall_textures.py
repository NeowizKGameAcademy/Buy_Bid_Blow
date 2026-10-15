from PIL import Image, ImageOps
import numpy as np
from pathlib import Path
p=Path(r'C:/course/unity/BuyBidBlow/Assets/Project/Content/Levels/Exchange/Textures')
sources=Path(r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/MaterialSources')
for source,target,color,strength in [('Floor_Source.jpg','Hall_Floor.png',(177,120,66),.65),('Wood_Source.jpg','Hall_Wood.png',(113,67,34),.4),('Plaster_Source.jpg','Hall_Teal.png',(39,83,79),.12),('Plaster_Source.jpg','Hall_Ceiling.png',(27,57,57),.1),('Brass_Source.jpg','Hall_Brass.png',(177,133,65),.22)]:
 im=Image.open(sources/source).convert('RGB').resize((1024,1024),Image.Resampling.LANCZOS)
 lum=np.asarray(ImageOps.grayscale(im),dtype=np.float32)/255
 factor=np.clip(1+(lum-lum.mean())*strength*2,.55,1.3)
 rgb=np.clip(factor[:,:,None]*np.array(color)[None,None,:],0,255).astype('uint8')
 Image.fromarray(rgb).save(p/target)
for n in ['Floor','Wood','Plaster','Brass']:
 im=Image.open(sources/(n+'_Normal.jpg')).convert('RGB').resize((1024,1024),Image.Resampling.LANCZOS)
 im.save(p/('Hall_'+n+'_Normal.png'))
print('Prepared 5 muted albedos and 4 normal maps at 1024px.')
