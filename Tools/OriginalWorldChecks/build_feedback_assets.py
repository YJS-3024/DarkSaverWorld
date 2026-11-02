"""Native attack marker conversion and source-linked frame crops; no gameplay defaults."""
import hashlib,importlib.util,json,re,uuid
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';OUT=Path(__file__).parent

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def build():
    source=BASE/'Art/Archives/gameres/etc.arc/mcursor.bmp.png';target=BASE/'Art/Combat/AttackMarker.png';target.parent.mkdir(exist_ok=True)
    image=Image.open(source).convert('RGB');result=Image.new('RGBA',image.size);result.putdata([(*p,0 if p==(0,0,0)else 255)for p in image.get_flattened_data()]);result.save(target)
    spec=importlib.util.spec_from_file_location('feedback_metadata',ROOT/'Tools/OriginalRestart/reset_assets.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    if not Path(str(target.parent)+'.meta').exists():module.meta(target.parent,BASE,True)
    identifier=uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+target.relative_to(BASE).as_posix()).hex
    template=Path(str(BASE/'Art/Tutorial/Attackeffects.png')+'.meta').read_text();Path(str(target)+'.meta').write_text(re.sub(r'^guid: .*$', 'guid: '+identifier,template,flags=re.M))
    for p in [BASE/'Runtime/Combat/BattleFeedback.cs',BASE/'Runtime/Combat/BattleFeedbackService.cs',BASE/'Runtime/Combat/BattleFeedbackFrames.cs',BASE/'Runtime/Entry/LocalServerManager.BattleFeedback.cs',BASE/'Runtime/World/WorldSceneRoot.BattleFeedback.cs',BASE/'Runtime/Combat/BattleLifecycle.cs',BASE/'Runtime/Combat/BattleLifecycleService.cs',BASE/'Runtime/Combat/NativeBodyPalette.cs',BASE/'Runtime/Combat/NativeMagicPackets.cs',BASE/'Runtime/Combat/NativeMagicService.cs',BASE/'Runtime/Combat/NativeMovement.cs',BASE/'Runtime/Combat/NativeMovementService.cs',BASE/'Runtime/Combat/NativeMoveHints.cs',BASE/'Runtime/Combat/NativeMoveHintService.cs',BASE/'Runtime/Combat/NativeBattleResponses.cs',BASE/'Runtime/Combat/NativeBattleResponseService.cs',BASE/'Runtime/Entry/LocalServerManager.NativeBattleResponses.cs',BASE/'Runtime/Combat/NativeBattlePackets.cs',BASE/'Runtime/Combat/NativeBattlePacketService.cs',BASE/'Runtime/Entry/LocalServerManager.NativeBattlePackets.cs',BASE/'Runtime/Combat/NativeBodyFrames.cs',BASE/'Runtime/Combat/NativeUnitFrames.cs',BASE/'Runtime/Combat/NativeViewport.cs',BASE/'Runtime/Combat/NativeBodyOrder.cs',BASE/'Runtime/World/WorldSceneRoot.BodyLayers.cs',BASE/'Runtime/Combat/DeathSoundPlayback.cs',BASE/'Runtime/Entry/LocalServerManager.BattleLifecycle.cs']:
        if not Path(str(p)+'.meta').exists():module.meta(p,BASE)
    asset=BASE/'Data/World/NativeWorldArt.asset';text='\n'.join(s for s in asset.read_text().splitlines()if not s.startswith('  attackMarker:'))+'\n';asset.write_text(text+'  attackMarker: {fileID: 2800000, guid: '+identifier+', type: 3}\n')
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=OUT/name
        if not p.exists():continue
        report=json.loads(p.read_text());key=asset.relative_to(ROOT).as_posix()
        if key in report.get('sources',{}):report['sources'][key]=sha(asset)
        for row in report.get('outputs',[]):
            if row['asset']==key:row['sha256']=sha(asset)
        p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    images={0:BASE/'Art/Tutorial/Board.png',1:BASE/'Art/Tutorial/Attackeffects.png',2:BASE/'Art/Tutorial/Effects.png',3:target};crops=[]
    def crop(surface,box):
        im=Image.open(images[surface]).convert('RGBA');x,y,w,h=box;assert x>=0 and y>=0 and x+w<=im.width and y+h<=im.height
        crops.append(dict(surface=surface,rect=box,pixelSha256=hashlib.sha256(im.crop((x,y,x+w,y+h)).tobytes()).hexdigest()))
    for row in range(4):
        for cell in range(1,13):crop(0,[227+cell*8,418+row*18,8,14])
    crop(0,[1,447,36,11])
    for frame in range(4):crop(1,[frame*32,32,32,32]);crop(2,[250+frame*32,240,32,32])
    for frame in range(2):crop(3,[255+frame*16,0,15,15])
    report=dict(source=source.relative_to(ROOT).as_posix(),sourceSha256=sha(source),output=target.relative_to(ROOT).as_posix(),outputSha256=sha(target),frames=crops,
        images={str(i):dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for i,p in images.items()},policy='Original pixel sizes and RGB black key.40ms is a configurable Unity presentation mapping, not an original measured update rate. No invented damage, MISS inference or HP/MP costs.')
    (OUT/'feedback-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    from build_body_palettes import build as build_palettes
    build_palettes()
    return dict(frames=len(crops),markerSize=list(image.size))
if __name__=='__main__':print(json.dumps(build()))
