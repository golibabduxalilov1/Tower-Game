"""Select supplied RGB+alpha artwork. ASC files are parsed as data; nothing is executed."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import io, json, re, zipfile, hashlib

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'assets/imported'
OUT.mkdir(parents=True, exist_ok=True)
ARCHIVE = Path.home() / 'Downloads/Fieldrunners2_barcha_dizaynlar (6).zip'
UI_ARCHIVE = Path.home() / 'Downloads/04_Iconlar_UI_va_ranglar.zip'
sources = [zipfile.ZipFile(ARCHIVE), zipfile.ZipFile(UI_ARCHIVE)]
index = {}
for z in sources:
    for entry in z.infolist():
        if entry.file_size > 32_000_000: continue
        name = re.sub(r'^\d+_', '', Path(entry.filename).name).lower()
        index.setdefault(name, (z, entry.filename))
used = {}
def raw(name):
    item = index.get(name.lower())
    if not item: return None
    z, entry = item
    used[entry] = {'archive': Path(z.filename).name, 'sha256': hashlib.sha256(z.read(entry)).hexdigest()}
    return z.read(entry)
def picture(stem):
    stem = re.sub(r'\.(jpg|png)$', '', stem, flags=re.I)
    rgb = raw(stem+'.jpg') or raw(stem+'.png')
    if not rgb: return None
    im = Image.open(io.BytesIO(rgb)).convert('RGBA')
    alpha = raw(stem+'_a.png')
    if alpha:
        mask = Image.open(io.BytesIO(alpha)).convert('L')
        if mask.size != im.size: raise ValueError('Mismatched mask '+stem)
        im.putalpha(mask)
    return im
def save(key, im):
    filename = key.replace(':','_').lower()+'.png'
    target = (OUT / filename).resolve()
    if not target.is_relative_to(OUT.resolve()): raise ValueError('Unsafe output path')
    im.save(target, optimize=True)
    manifest['assets'][key] = {'file': filename, 'width': im.width, 'height': im.height}

manifest={'version':1,'source':'User-supplied Fieldrunners2 archives; selected compatible artwork','assets':{}}
mapping={'Gatling':'Gatling','Goo':'Glue','Missile':'Missile','Tesla':'Tesla','Flame':'Flame','Mortar':'Cannonball','Laser':'Laser','Ice':'Ice'}
for kind, original in mapping.items():
    icon=picture('Icon_Tower_'+original)
    if icon: save('icon:'+kind,icon)

def frames(asc):
    data=raw(asc)
    if not data:return []
    lines=data.decode('utf-8').splitlines()
    current_file=None; aset='default'; anim=''; orient=0; output=[]; i=0; anchor_names={}
    while i<len(lines):
        line=lines[i].strip();i+=1
        if re.fullmatch(r'\d+ [A-Za-z][A-Za-z0-9_]*',line):
            number,label=line.split(); anchor_names[label]=int(number); continue
        if line.startswith('file '):current_file=line.split()[1];continue
        if line.startswith('anim_set '):aset=line.split()[1];continue
        if line.startswith('anim '):anim=line.split()[1];continue
        if line.startswith('orientation '):orient=int(line.split()[1]);continue
        # image record: frameNumber x y width height anchorCount
        if re.fullmatch(r'\d+ \d+ \d+ \d+ \d+ \d+',line) and current_file:
            number,x,y,w,h,n=map(int,line.split())
            if w<1 or h<1 or w>2048 or h>2048 or n>32:continue
            anchors={}
            for _ in range(n):
                while i<len(lines) and not lines[i].strip():i+=1
                if i>=len(lines):break
                parts=lines[i].split();i+=1
                if len(parts)>=3:
                    try:anchors[int(parts[0])]=(float(parts[1]),float(parts[2]))
                    except ValueError:pass
            output.append(dict(file=current_file,aset=aset,anim=anim,orient=orient,rect=(x,y,w,h),anchor=anchors.get(0,(w/2,h*.7)),health=anchors.get(anchor_names.get('health',4),(w/2,0)),props=[anchors[anchor_names[label]] for label in ('prop','prop2') if label in anchor_names and anchor_names[label] in anchors]))
    return output

def choose(fs,level,angle):
    lev=[f for f in fs if f['aset']==f'level{level}']
    if not lev:lev=[f for f in fs if (f'level{level}' in f['anim'] or level==1 and f['anim'] in ('idle','run'))]
    if not lev:lev=fs
    idle=[f for f in lev if f['anim'] in ('run','idle') or f['anim'].startswith('idle')]
    pool=idle or lev
    return min(pool,key=lambda f:abs(f['orient']-angle)) if pool else None

sheet_cache={}
def crop(frame):
    name=frame['file']
    if name not in sheet_cache:sheet_cache[name]=picture(name)
    sheet=sheet_cache[name]
    if sheet is None:return None
    x,y,w,h=frame['rect']
    if x+w>sheet.width or y+h>sheet.height:return None
    return sheet.crop((x,y,x+w,y+h))

towers={
 'Gatling':('Tower_Gatling_Turret.asc','Tower_MachineGun_Base.asc'),
 'Goo':('Tower_Glue_Turret.asc','Tower_Glue_Base.asc'),
 'Missile':('Tower_Missile.asc',None),
 'Tesla':('Tower_Lightning.asc',None),
 'Flame':('Tower_Flame_Turret.asc','Tower_Flame_Base.asc'),
 'Mortar':('Tower_Cannonball_Turret.asc','Tower_Cannonball_Base.asc'),
 'Laser':('Tower_Laser_Turret.asc','Tower_Laser_Base.asc'),
 'Ice':('Tower_Ice_Turret.asc','Tower_Ice_Base.asc')}
for kind,(head_name,base_name) in towers.items():
    heads=frames(head_name); bases=frames(base_name) if base_name else []
    if not heads:print('No head data:',kind);continue
    # Keep source pixels: crop transparent padding rather than halve every sprite.
    frame_size=192
    crop_box=(32,40,224,232)
    atlas=Image.new('RGBA',(frame_size*19,frame_size*3))
    valid=True
    for level in range(1,4):
        for angle in range(0,181,10):
            image=Image.new('RGBA',(256,256))
            for record in [choose(bases,level,0),choose(heads,level,angle)]:
                if not record:continue
                item=crop(record)
                if item is None:
                    valid=False;continue
                ax,ay=record['anchor'];image.alpha_composite(item,(round(128-ax),round(154-ay)))
            bounds=image.getbbox()
            if bounds and (bounds[0]<crop_box[0] or bounds[1]<crop_box[1] or bounds[2]>crop_box[2] or bounds[3]>crop_box[3]):
                raise ValueError(f'Sprite exceeds native crop: {kind} level{level} angle{angle}: {bounds}')
            atlas.alpha_composite(image.crop(crop_box),(angle//10*frame_size,(level-1)*frame_size))
    if atlas.getbbox():
        # 3648x576 stays below Reach4096 while retaining native source detail.
        save('tower:'+kind,atlas)
        manifest['assets']['tower:'+kind].update(cell=192,columns=19,rows=3,anchorX=96,anchorY=114,drawSize=256)

# Authentic humanoid running cycles: preserve native poses and directional perspective.
unit_sources = {
    'Infantry':'Enemy_Soldier_Light', 'Runner':'Enemy_Soldier_Anti_Slow_Light', 'Heavy':'Enemy_Soldier_Heavy',
    'Motorcycle':'Enemy_Bike_Light', 'Tank':'Enemy_Tank_Light',
    'Helicopter':'Enemy_Air_Helicopter_Light', 'Plane':'Enemy_Air_Plane_Bi_Medium',
    'RailCart':'Enemy_Truck_Transport_Medium', 'HeavyTank':'Enemy_Hero_Tank_Tiger',
    'AirFortress':'Enemy_Air_Helicopter_Transport_Heavy', 'RailColossus':'Enemy_Truck_V2_Heavy'
}
for kind,stem in unit_sources.items():
    runs=[f for f in frames(stem+'.asc') if f['anim']=='run']
    prop_stem = 'Enemy_Air_Helicopter_Propeller' if kind in ('Helicopter','AirFortress') else 'Enemy_Air_Plane_Propeller' if kind=='Plane' else None
    props=[f for f in frames(prop_stem+'.asc') if f['anim']=='run'] if prop_stem else []
    if props:
        runs=[dict(f,propFrame=prop) for f in runs for prop in props if prop['orient']==min(props,key=lambda p:abs(p['orient']-f['orient']))['orient']]
    if not runs: raise ValueError('Missing soldier run '+kind)
    min_x=min(-f['anchor'][0] for f in runs); min_y=min(-f['anchor'][1] for f in runs)
    max_x=max(f['rect'][2]-f['anchor'][0] for f in runs); max_y=max(f['rect'][3]-f['anchor'][1] for f in runs)
    cell=int((max(max_x-min_x,max_y-min_y)+(87 if props else 23))//8*8)
    ax=round((cell-(max_x-min_x))/2-min_x); ay=round((cell-(max_y-min_y))/2-min_y)
    columns=min(16,4096//cell); rows=(len(runs)+columns-1)//columns
    if rows*cell>4096: raise ValueError("Unit atlas exceeds Reach limit: "+kind)
    atlas=Image.new('RGBA',(columns*cell,rows*cell)); directions=[]; health_offsets=[]; index_frame=0
    for angle in sorted({f['orient'] for f in runs}):
        group=[f for f in runs if f['orient']==angle]
        directions.append({'angle':angle,'start':index_frame,'count':len(group)})
        for f in group:
            item=crop(f)
            if item is None: raise ValueError('Missing soldier image '+f['file'])
            frame=Image.new('RGBA',(cell,cell)); x,y=f['anchor']
            frame.alpha_composite(item,(round(ax-x),round(ay-y)))
            if 'propFrame' in f:
                prop=f['propFrame']; prop_image=crop(prop)
                for px,py in f['props']:
                    frame.alpha_composite(prop_image,(round(ax+px-x-prop['anchor'][0]),round(ay+py-y-prop['anchor'][1])))
            if not frame.getbbox(): raise ValueError('Empty soldier pose '+kind)
            atlas.alpha_composite(frame,((index_frame%columns)*cell,(index_frame//columns)*cell))
            health_offsets.append(round(f['health'][1]-f['anchor'][1],3));index_frame+=1
    save('soldier:'+kind,atlas)
    manifest['assets']['soldier:'+kind].update(cell=cell,columns=columns,rows=rows,anchorX=ax,anchorY=ay,
        referenceHeight=round(max_y-min_y),directions=directions,healthOffsets=health_offsets,framesPerSecond=30,frameCount=index_frame)
    print(f'{kind}: {index_frame} running poses, {len(directions)} directions, native cell{cell}')

decor={
 'nature:tree-wide':'G2_Overlay_Tree_04',
 'nature:tree-round':'G2_Overlay_Tree_05',
 'nature:garden':'G4_Overlay_Bottom_1',
 'nature:hedge':'G4_Overlay_Bottom_2',
 'nature:palm-wide':'D3_Overlay_Tree_01',
 'nature:palm-low':'D3_Overlay_Tree_02',
 'nature:palm-tall':'D3_Overlay_Tree_03',
 'nature:dry-tree':'D1_Overlay_Tree_02',
 'nature:plant-small':'D1_Overlay_Plant_01',
 'nature:cactus2':'D5_Overlay_Plant_02',
 'nature:cactus3':'D5_Overlay_Plant_03',
 'nature:cactus4':'D5_Overlay_Plant_04',
 'nature:cactus5':'D5_Overlay_Plant_05',
 'nature:cactus6':'D5_Overlay_Plant_06',
 'nature:cactus7':'D5_Overlay_Plant_07',
 'decor:tree':'G2_Overlay_Tree_01',
 'decor:tree2':'G2_Overlay_Tree_02',
 'decor:desert-tree':'D1_Overlay_Tree_01',
 'decor:desert-hut':'D1_Overlay_Hut_01',
 'decor:desert-plant':'D5_Overlay_Plant_01',
 'decor:crystal':'l1_Overlay_Crystal_C_01',
 'decor:crystal2':'l1_Overlay_Crystal_C_04',
 'decor:fence':'G2_Overlay_Fence',
 'decor:border':'G1_Overlay_Boarder_S_01',
 'decor:entrance':'G1_Overlay_Entrance_01',
 'decor:exit':'G1_Overlay_Exit_01',
 'decor:rock':'l2_Overlay_Rocks_e',
 'world:island1':'Worldmap_Island_01',
 'world:island2':'Worldmap_Island_02',
 'world:background1':'Worldmap_Background_01',
 'world:background2':'Worldmap_Background_02',
 'world:stars':'Worldmap_Stars_00000'}
for key,name in decor.items():
    im=picture(name)
    if im:
        im.thumbnail((1536,1536),Image.Resampling.LANCZOS)
        save(key,im)

# Individual native foliage fragments from the supplied garden strips.
for key,stem,rect in [
    ('nature:berry-bush','G4_Overlay_Bottom_1',(249,16,349,110)),
    ('nature:shrub','G2_Overlay_Tree_02',(399,15,455,87)),
    ('nature:clover','G2_Overlay_Tree_02',(89,35,145,89))]:
    source=picture(stem)
    if source:
        fragment=source.crop(rect)
        # Strip crops contain pieces of adjacent soil/fence. Isolate the foliage,
        # preserving native RGB pixels and berries instead of exposing square cut edges.
        for y in range(fragment.height):
            for x in range(fragment.width):
                r,g,b,a=fragment.getpixel((x,y))
                foliage=(g>r*1.04 and g>b*1.04) or (b>r*1.2 and b>g*1.05)
                d=((x-fragment.width*.5)/(fragment.width*.49))**2+((y-fragment.height*.49)/(fragment.height*.50))**2
                edge=max(0,min(1,(1-d)*24))
                fragment.putpixel((x,y),(r,g,b,round(a*edge) if foliage else 0))
        save(key,fragment)

# Newly generated high-resolution foliage replaces the small archive silhouettes.
# Slice the atlas at its transparent gutters; retain the original generated alpha.
hd_source=ROOT/'assets/art-source/foliage-hd-v1.png'
if hd_source.exists():
    hd=Image.open(hd_source).convert('RGBA')
    for key,rect in [
        ('nature:hd-oak-a',(0,0,674,704)),
        ('nature:hd-oak-b',(674,0,1254,704)),
        ('nature:hd-berry',(0,704,627,1254)),
        ('nature:hd-fern',(627,704,1254,1254))]:
        sprite=hd.crop(rect)
        bbox=sprite.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
        if not bbox: raise ValueError('Empty generated foliage '+key)
        sprite=sprite.crop(bbox)
        padded=Image.new('RGBA',(sprite.width+16,sprite.height+16))
        padded.alpha_composite(sprite,(8,8))
        save(key,padded)

# Menu-resolution illustration based on the actual game units; source identity is documented.
duel_source=ROOT/'assets/art-source/menu-native-duel-v2-vs.png'
if not duel_source.exists(): duel_source=ROOT/'assets/art-source/menu-native-duel-v1.png'
if duel_source.exists():
    save('menu:duel-native-hd',Image.open(duel_source).convert('RGBA'))

# Full-page menu scene extends the same ground beneath the controls and the units.
scene_source=ROOT/'assets/art-source/menu-full-scene-v1.png'
if scene_source.exists():
    save('menu:scene',Image.open(scene_source).convert('RGBA'))

(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(OUT/'provenance.json').write_text(json.dumps(used,indent=2),encoding='utf-8')
# A local visual inventory makes transparency/frame selection reviewable.
tiles=[]
for key,entry in manifest['assets'].items():
    im=Image.open(OUT/entry['file']).convert('RGBA')
    if key.startswith('tower:'):
        cell=entry['cell'];im=im.crop((9*cell,2*cell,10*cell,3*cell))
    tile=Image.new('RGBA',(240,220),(57,75,68,255));im.thumbnail((226,180),Image.Resampling.LANCZOS)
    tile.alpha_composite(im,((240-im.width)//2,8+(180-im.height)//2))
    ImageDraw.Draw(tile).text((8,197),key,fill='white')
    tiles.append(tile)
sheet=Image.new('RGBA',(240*6,220*((len(tiles)+5)//6)),(32,40,38,255))
for i,t in enumerate(tiles):sheet.alpha_composite(t,((i%6)*240,(i//6)*220))
sheet.save(OUT/'contact-sheet.png')
print(f"Imported {len(manifest['assets'])} selected assets from {len(used)} supplied source files.")
