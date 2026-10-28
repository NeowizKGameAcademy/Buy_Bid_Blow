import fs from 'node:fs/promises';
import {Presentation,PresentationFile} from '@oai/artifact-tool';
import {build,BASE} from './build.mjs';
const p=Presentation.create({slideSize:{width:1280,height:720}});await build(p);
await fs.mkdir(BASE+'/renders',{recursive:true});
for(let i=0;i<p.slides.items.length;i++){const b=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(BASE+'/renders/page-'+String(i+1).padStart(2,'0')+'.png',new Uint8Array(await b.arrayBuffer()));}
await(await PresentationFile.exportPptx(p)).save(BASE+'/candidate.pptx');
console.log('Rendered and exported '+p.slides.items.length+' slides');
