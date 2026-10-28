import fs from 'node:fs/promises';
import {Presentation} from '@oai/artifact-tool';
const root='C:/훈련생폴더/3. 유니티/Buy_Bid_Blow/Docs/TechnicalPresentation';
const p=await Presentation.load(root+'/Output/Buy_Bid_Blow_기술문서_v3.pptx');
await fs.mkdir(root+'/.revision/before',{recursive:true});
const list=[];
for(let i=0;i<p.slides.items.length;i++){
 const s=p.slides.items[i];list.push({n:i+1,id:s.id,shapes:s.shapes.items.map(x=>({id:x.id,text:String(x.text),pos:{left:x.position.left,top:x.position.top,width:x.position.width,height:x.position.height}})),images:s.images.items.map(x=>({id:x.id,pos:{left:x.position.left,top:x.position.top,width:x.position.width,height:x.position.height}}))});
 const b=await p.export({slide:s,format:'png',scale:.5});await fs.writeFile(root+'/.revision/before/'+(i+1)+'.png',new Uint8Array(await b.arrayBuffer()));
}
await fs.writeFile(root+'/.revision/inspect.json',JSON.stringify(list,null,2));
console.log(list.map(x=>[x.n,...x.shapes.map(y=>y.text).filter(Boolean)].join(' :: ')).join('\n'));
