"""Reinterpret original deployment as commander/slot/formation, not spatial clustering.
Read-only for game content. Emits version-scoped evidence and an analysis viewer.
"""
import collections
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / '분석/Saver_Late'
OUT = ROOT / 'Tools/ScenarioPlacementChecks'
sys.path.insert(0, str(ROOT / 'Tools/OriginalClientChecks'))
from native_pe import PE


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def ai_records(path):
    result = []
    if not path.exists():
        return result
    for line, text in enumerate(path.read_text(encoding='utf-8').splitlines(), 1):
        match = re.fullmatch(r'\s*(\d+)\s+(OD\w+)\s+(AREA|SOILDER)\s*\(([^)]*)\)\s*(.*)', text)
        if match:
            unit, mode, selector, args, actions = match.groups()
            result.append(dict(unit=int(unit), mode=mode, selector=selector,
                               args=[int(x) for x in args.split(',')], line=line,
                               actions=actions.strip(), raw=text))
        elif text.split(';', 1)[0].strip() not in ('', '999'):
            raise ValueError((path, line, text))
    return result


def pos_records(path):
    result = []
    block = 0
    for line, text in enumerate(path.read_text(encoding='utf-8').splitlines(), 1):
        text = text.split(';', 1)[0].strip()
        if not text:
            block += 1
            continue
        tokens = text.split()
        if tokens == ['999']:
            continue
        assert len(tokens) == 3 and all(x.isdigit() for x in tokens), (path, line)
        unit, x, y = map(int, tokens)
        result.append(dict(unit=unit, x=x, y=y, line=line, block=block))
    assert len({r['unit'] for r in result}) == len(result), path
    return result


def groups_for(units):
    groups = collections.defaultdict(list)
    for unit in units:
        groups[unit['unit'] // 10 * 10].append(unit)
    result = []
    for commander, members in sorted(groups.items()):
        members.sort(key=lambda u: u['unit'])
        leader = next((u for u in members if u['unit'] == commander), None)
        slots = [dict(unit=u['unit'], slot=u['unit'] % 10,
                      offset=[u['x'] - leader['x'], u['y'] - leader['y']] if leader else None)
                 for u in members]
        result.append(dict(commander=commander, leaderPosition=[leader['x'], leader['y']] if leader else None,
                           memberCount=len(members), members=slots,
                           waves=sorted({u.get('wave', 0) for u in members})))
    return result


def native_code_evidence():
    inventory = json.loads((ROOT / '분석/InstallerVersions/installer_inventory.json').read_text(encoding='utf-8'))
    late = next(v for v in inventory['payloads'] if 'saver.exe' in v['containers'])
    record = next(f for f in late['files'] if f['file'] == 'userunit.dll')
    pe = PE(Path(record['extractedFile']))
    assert pe.sha256 == '74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b'
    pe.expect(0x10002444, 'b9 0a 00 00 00 f7 f9 85 d2 75 0f')
    pe.expect(0x100015a4, 'b8 67 66 66 66 f7 e9 c1 fa 02')
    pe.expect(0x100035e9, 'b8 67 66 66 66 f7 ed c1 fa 02')
    pe.expect(0x10003609, '8d 04 92 57 d1 e0')
    pe.expect(0x10004932, '83 f8 01')
    pe.expect(0x10004965, '8b 41 70 8b 51 74 8b 49 4c')
    exports = pe.exports()
    assert pe.thunk(exports['?_ai@@YAXH@Z']) == 0x10002420
    assert pe.thunk(exports['?setmonsterai@@YAXH@Z']) == 0x100015a0
    evidence = OUT / 'evidence/late-troop-grouping.asm.txt'
    evidence.parent.mkdir(exist_ok=True)
    ranges = [(0x100015a0, 0x1000160b), (0x10002420, 0x1000249d),
              (0x100035e0, 0x100037d0), (0x10004810, 0x10004981)]
    evidence.write_text('\n'.join(pe.disasm(a, b) for a, b in ranges), encoding='utf-8')
    # Check the signed compiler division expression for the supported nonnegative unit domain.
    for unit in range(1000):
        high = unit * 0x66666667 >> 32
        quotient = high >> 2
        assert quotient == unit // 10
    return dict(file='userunit.dll', sha256=pe.sha256,
                evidence=str(evidence.relative_to(ROOT)),
                dispatch='0x10002444: unit%10==0 -> ExecuteCommanderAI, else soldier AI',
                leaderLookup='0x100015a4 / 0x100035e9: trunc(unit/10)*10 for nonnegative IDs',
                soldierMovement='0x10003634..0x10003662: Manhattan distance to that commander, compared to commander+0x4c',
                targetMovement='0x10004932..0x1000497c: SOILDER selector uses target current +0x70/+0x74 and radius max(target+0x4c,7)',
                execution='Static opcode/export checks and arithmetic model; not native execution or emulation')


def main():
    source_path = BASE / 'scenario_monster_placements.json'
    source = json.loads(source_path.read_text(encoding='utf-8'))
    maps = {Path(m['path']).stem: m for m in json.loads((BASE / 'maps.json').read_text(encoding='utf-8'))}
    shapes = collections.Counter()
    scenarios = []
    for scenario in source['scenarios']:
        key = scenario['scenario']
        folder = BASE / f'decoded/map/{key}set.arc'
        pos = pos_records(folder / f'{key}.pos')
        units = scenario['units']
        ids = {u['unit'] for u in units}
        for ext in ('set', 'pos'):
            assert digest(folder / f'{key}.{ext}') == scenario[ext + 'Sha256']
        # Recheck the original SET/POS join rather than rely solely on the previous JSON.
        positions = {p['unit']: p for p in pos}
        definitions = {}
        for line in (folder / f'{key}.set').read_text(encoding='utf-8').splitlines():
            tokens = line.split(';', 1)[0].split()
            if len(tokens) in (2, 3) and all(t.isdigit() for t in tokens):
                definitions[int(tokens[0])] = int(tokens[1])
        assert definitions == {u['unit']: u['job'] for u in units}
        assert all((u['x'], u['y']) == (positions[u['unit']]['x'], positions[u['unit']]['y']) for u in units)
        groups = groups_for(units)
        ai = ai_records(folder / f'{key}.ai')
        area = {r['unit']: r for r in ai if r['selector'] == 'AREA'}
        eligible = {k: v for k, v in area.items() if k in ids}
        displaced, nearest_errors = [], []
        for group in groups:
            offsets = tuple(sorted(tuple(m['offset']) for m in group['members']))
            shapes[str(offsets)] += 1
            a = area.get(group['commander'])
            if a and a['args'][:2] != group['leaderPosition']:
                displaced.append(dict(commander=group['commander'], pos=group['leaderPosition'], area=a['args'], line=a['line']))
        for u in units:
            if not eligible:
                continue
            # Mirror the editor comparison policy, including commanders keeping their own area ID.
            nearest = u['unit'] if u['unit'] in eligible else min(eligible, key=lambda k: ((u['x']-eligible[k]['args'][0])**2 + (u['y']-eligible[k]['args'][1])**2, k))
            if nearest != u['unit'] // 10 * 10:
                nearest_errors.append(dict(unit=u['unit'], commander=u['unit']//10*10, nearestArea=nearest))
        scenarios.append(dict(key=key, kind='original SET/POS textual join', width=maps[key]['width'], height=maps[key]['height'],
                              units=units, groups=groups, ai=ai, areaPosMismatch=displaced,
                              nearestAreaMisassigned=nearest_errors,
                              posWithoutSet=[p for p in pos if p['unit'] not in ids],
                              sources={ext: dict(path=str((folder/f'{key}.{ext}').relative_to(ROOT)), sha256=digest(folder/f'{key}.{ext}')) for ext in ('set','pos','ai')}))
    authored = []
    for number in range(1, 18):
        key = f'{number:02d}'
        path = ROOT / f'Assets/_Game/Resources/Scenarios/{key}/Content.json'
        content = json.loads(path.read_text(encoding='utf-8'))
        units = [dict(u, unit=u['id']) for u in content['units']]
        groups = groups_for(units)
        cells = collections.defaultdict(list)
        for u in units:
            cells[u['x'], u['y']].append(u)
        crosswave = [dict(position=list(cell), units=[u['unit'] for u in members], waves=sorted({u['wave'] for u in members}))
                     for cell, members in cells.items() if len({u['wave'] for u in members}) > 1]
        folder = BASE / f'decoded/map/{key}set.arc'
        authored.append(dict(key=key, kind='current authored content; not recovered original deployment',
                             width=content['width'], height=content['height'], units=units, groups=groups,
                             ai=ai_records(folder/f'{key}.ai'), crossWaveOverlaps=crosswave,
                             source=dict(path=str(path.relative_to(ROOT)), sha256=digest(path))))
    ai_all = []
    for path in sorted((BASE/'decoded/map').glob('*set.arc/*.ai')):
        rows = ai_records(path)
        ai_all.extend(dict(r, scenario=path.stem) for r in rows)
    report = dict(scope='Commander identity, member slot, literal formation and AI movement references are separate. No game data changed.',
                  code=native_code_evidence(), sourceSha256=digest(source_path),
                  summary=dict(nativeScenarios=len(scenarios), nativeUnits=sum(len(s['units']) for s in scenarios),
                               nativeGroups=sum(len(s['groups']) for s in scenarios),
                               shapes=dict(shapes), areaPosMismatches=sum(len(s['areaPosMismatch']) for s in scenarios),
                               nearestAreaMisassigned=sum(len(s['nearestAreaMisassigned']) for s in scenarios),
                               posWithoutSet=sum(len(s['posWithoutSet']) for s in scenarios),
                               selectors=dict(collections.Counter(r['selector'] for r in ai_all))),
                  scenarios=scenarios, authoredScenarios=authored,
                  soldierTargetReferences=[r for r in ai_all if r['selector']=='SOILDER'],
                  interpretation=['ID decade grouping is code-backed for this late client, not merely a proximity guess.',
                                  'Observed roster size five does not establish a five-unit runtime limit; slots1..9 can refer to the same commander.',
                                  'POS is literal position plus observed formation offsets; do not add commander position to already-absolute values.',
                                  'AREA governs movement region; SOILDER supplies a moving reference unit, not a reassignment of commander ownership.',
                                  'Unjoined POS rows are not extra monsters. Missing initial SET/POS in01..17 stays missing.',
                                  'Current authored waves and all-wave overlap are separate from original simultaneous deployment.'])
    target = BASE / 'troop_structure_review.json'
    target.write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    write_viewer(report)
    write_plot(report)
    # The analysis never rewrites Unity content.
    assert all(digest(ROOT/s['source']['path']) == s['source']['sha256'] for s in authored)
    print(json.dumps(report['summary'], ensure_ascii=False))


def write_viewer(report):
    data = json.dumps(report, ensure_ascii=False).replace('<','\\u003c')
    html = '''<!doctype html><html lang="ko"><meta charset="utf-8"><title>몬스터 부대 구조 대조</title>
<style>body{font:15px system-ui;background:#101923;color:#e3ecf3;margin:24px}select,label{margin:6px}select{padding:7px}svg{background:#182533;max-height:75vh;width:100%;touch-action:none}#info{white-space:pre-line;line-height:1.7}circle,polygon{stroke:#fff;stroke-width:.08}line{pointer-events:none}button{padding:6px}</style>
<h2>몬스터 부대 구조 대조</h2><p>원본 SET/POS와 현재 보완 배치를 구분합니다. 점선은 SOILDER 이동 참조이며 부대 소유 관계가 아닙니다. AI 중심은 출현 위치로 보정하지 않습니다.</p>
<select id="map"></select><select id="mode"><option value="id">코드 기준 지휘관/부하</option><option value="near">가까운 AI 중심 가설</option></select>
<label><input id="areas" type="checkbox" checked>AI 중심</label><label><input id="names" type="checkbox" checked>ID</label><select id="wave"><option value="all">모든 웨이브</option></select>
<p>◆ 지휘관 슬롯(끝자리0) / ● 부하 슬롯 / ＋ AREA 중심 / 색상: 묶음 구분</p><svg id="view"></svg><div id="info"></div>
<script>const data=DATA;const maps=[...data.scenarios,...data.authoredScenarios];
const $=id=>document.getElementById(id);maps.forEach((s,i)=>$('map').add(new Option(s.key+' · '+(s.kind.startsWith('original')?'원본 SET/POS':'현재 보완 배치'),i)));
const svg=$('view'),ns='http://www.w3.org/2000/svg';function el(tag,a,text){let n=document.createElementNS(ns,tag);Object.entries(a).forEach(([k,v])=>n.setAttribute(k,v));if(text!==undefined)n.textContent=text;svg.append(n);return n}
function color(id){return `hsl(${(id*0.618034*360)%360} 68% 65%)`}
function refresh(){let s=maps[+$('map').value];$('wave').innerHTML='<option value="all">모든 웨이브</option>';[...new Set(s.units.map(u=>u.wave??0))].sort((a,b)=>a-b).forEach(w=>$('wave').add(new Option('웨이브 '+w,w)));draw()}
function draw(){let s=maps[+$('map').value],w=$('wave').value,us=s.units.filter(u=>w==='all'||(u.wave??0)===+w);svg.replaceChildren();svg.setAttribute('viewBox',`-1 -1 ${s.width+2} ${s.height+2}`);
for(let x=0;x<=s.width;x++)el('line',{x1:x-.5,y1:-.5,x2:x-.5,y2:s.height-.5,stroke:'#2d3e50','stroke-width':.025});for(let y=0;y<=s.height;y++)el('line',{x1:-.5,y1:y-.5,x2:s.width-.5,y2:y-.5,stroke:'#2d3e50','stroke-width':.025});
let areas=s.ai.filter(a=>a.selector==='AREA'&&us.some(u=>u.unit===a.unit));let owner=u=>{if($('mode').value==='id'||!areas.length)return Math.floor(u.unit/10)*10;if(areas.some(a=>a.unit===u.unit))return u.unit;return [...areas].sort((a,b)=>((u.x-a.args[0])**2+(u.y-a.args[1])**2)-((u.x-b.args[0])**2+(u.y-b.args[1])**2)||a.unit-b.unit)[0].unit};
for(let u of us){let id=owner(u),leader=us.find(v=>v.unit===id&&(v.wave??0)===(u.wave??0));if(leader&&u!==leader)el('line',{x1:u.x,y1:u.y,x2:leader.x,y2:leader.y,stroke:color(id),'stroke-width':.065,opacity:.75})}
for(let a of s.ai.filter(a=>a.selector==='SOILDER')){let u=us.find(u=>u.unit===a.unit),v=us.find(u=>u.unit===a.args[0]);if(u&&v)el('line',{x1:u.x,y1:u.y,x2:v.x,y2:v.y,stroke:'#fff','stroke-width':.08,'stroke-dasharray':'.2 .15'})}
for(let u of us){let c=color(owner(u)),n=u.unit%10===0?el('polygon',{points:`${u.x},${u.y-.34} ${u.x+.34},${u.y} ${u.x},${u.y+.34} ${u.x-.34},${u.y}`,fill:c}):el('circle',{cx:u.x,cy:u.y,r:.23,fill:c});let t=document.createElementNS(ns,'title');t.textContent=`ID ${u.unit} / job ${u.job} / (${u.x},${u.y}) / W${u.wave??0}`;n.append(t);if($('names').checked)el('text',{x:u.x+.35,y:u.y+.15,fill:c,'font-size':.55},u.unit)}
if($('areas').checked)for(let a of areas){let [x,y]=a.args;el('text',{x,y,fill:'#9af5ff','font-size':.8,'text-anchor':'middle'},'+');el('text',{x:x+.5,y:y-.3,fill:'#9af5ff','font-size':.5},'A'+a.unit)}
let warn=s.kind.startsWith('original')?`원문 ${s.units.length}마리 / ${s.groups.length}부대\nPOS≠AREA ${s.areaPosMismatch.length}건 / 근접 가설 소속 불일치 ${s.nearestAreaMisassigned.length}마리\nSET 없는 POS ${s.posWithoutSet.length}행 (추가 몬스터로 판정하지 않음)`:`현재 보완 배치 ${s.units.length}마리 / ${s.groups.length}묶음\n원작 초기 편성으로 확정할 수 없습니다. 웨이브 간 중첩 칸 ${s.crossWaveOverlaps.length}개`;
$('info').textContent=warn;}
for(let id of ['mode','areas','names','wave'])$(id).onchange=draw;$('map').onchange=refresh;refresh();</script></html>'''
    # Inline data makes the read-only viewer work without a local server.
    html = html.replace('const data=DATA;', 'const data='+data+';')
    (OUT/'troop-structure-view.html').write_text(html, encoding='utf-8')


def write_plot(report):
    import os
    os.environ.setdefault('MPLCONFIGDIR', '/tmp/darksaver-placement-matplotlib')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from matplotlib.colors import hsv_to_rgb
    fig = plt.figure(figsize=(15, 10), facecolor='#101923')
    layout = fig.add_gridspec(2, 4, height_ratios=[2.5, 1])
    sample = next(s for s in report['scenarios'] if s['key'] == '26')
    errors = {r['unit']: r['nearestArea'] for r in sample['nearestAreaMisassigned']}
    def color(i):
        return hsv_to_rgb(((i*.618034)%1, .65, .95))
    for panel in range(2):
        ax = fig.add_subplot(layout[0, panel*2:panel*2+2])
        by_id = {u['unit']: u for u in sample['units']}
        for u in sample['units']:
            owner = errors.get(u['unit'], u['unit']//10*10) if panel else u['unit']//10*10
            leader = by_id[owner]
            ax.plot([u['x'], leader['x']], [u['y'], leader['y']], color=color(owner), lw=.9, alpha=.6)
            ax.scatter(u['x'], u['y'], color=color(owner), marker='D' if u['unit']%10==0 else 'o', s=40)
            if u['unit']%10==0 or u['unit'] in errors:
                ax.annotate(str(u['unit']), (u['x'],u['y']), xytext=(4,-9), textcoords='offset points', fontsize=8, color='white')
        ax.set(xlim=(-1,sample['width']), ylim=(sample['height'],-1), aspect='equal')
        ax.set_title('26: code-backed commander + member slots' if not panel else '26: nearest AI center (8 wrong member links)', color='white', pad=15)
        ax.set_xlabel('Original POS X', color='white'); ax.set_ylabel('Original POS Y', color='white')
    for n, (shape, count) in enumerate(report['summary']['shapes'].items()):
        import ast
        offsets = ast.literal_eval(shape)
        ax = fig.add_subplot(layout[1, n])
        for x,y in offsets:
            ax.scatter(x,y,s=100,marker='D' if (x,y)==(0,0) else 'o',color='#ffd27b' if (x,y)==(0,0) else '#7fd4ed')
        ax.set(xlim=(-3,3),ylim=(3,-3),aspect='equal',xticks=range(-2,3),yticks=range(-2,3))
        ax.set_title(f'Observed relative shape: {count} groups', color='white', fontsize=10)
    for ax in fig.axes:
        ax.set_facecolor('#182533');ax.grid(alpha=.13,color='white');ax.tick_params(colors='#b4c5d5')
        for spine in ax.spines.values():spine.set_color('#405267')
    fig.suptitle('Original troop layout: identity, position and movement region are different data',color='white',fontsize=15)
    fig.text(.5,.015,'Read-only analysis. Shapes: 122 source groups / 610 members. Not an invented deployment for scenarios 01-17.',ha='center',color='#b4c5d5')
    fig.tight_layout(rect=[0,.04,1,.96])
    fig.savefig(OUT/'troop-structure-comparison.png',dpi=135,facecolor=fig.get_facecolor())
    plt.close(fig)


if __name__ == '__main__':
    main()
