"""Validate authoring documents/config only. Does not run Unity or certify gameplay."""
from pathlib import Path
from collections import Counter
import json
import re
import hashlib
import sys

ROOT=Path(__file__).resolve().parents[1]
CFG=ROOT/'FullGame_Config_1.0.json'
c=json.loads(CFG.read_text(encoding='utf-8'))
checks=[]
def check(name,ok,detail=''):
    checks.append((name,bool(ok),str(detail)))
def unique(rows):
    ids=[v['id'] for v in rows]
    return len(ids)==len(set(ids))
species={s['id']:s for s in c['species']}
items={s['id']:s for s in c['items']}
contracts={s['id']:s for s in c['contracts']}
maps={s['id']:s for s in c['maps']}
rare={s['speciesId']:s for s in c['rareEvents']}
stars=[s for ct in c['contracts'] for s in ct['starPredicates']]
check('JSON有效且明确为作者规格',c['status']=='authoring-spec-not-imported-not-implemented')
check('7场地=公司+6作业图',set(maps)=={'Company',*[f'L{x:02}' for x in range(1,7)]})
check('18合同/54独立挑战',len(contracts)==18 and len(stars)==54 and len({s['id'] for s in stars})==54 and all(len(x['starPredicates'])==3 for x in c['contracts']))
groups=Counter(s['id'][0] for s in c['species'])
check('45条图鉴类别计数',groups=={'I':14,'M':17,'S':2,'C':12},dict(groups))
check('30物品/29占格/1常驻',len(items)==30 and sum(x['emptySlots']>0 for x in items.values())==29 and items['E14']['emptySlots']==0)
platforms={x['platformId'] for x in items.values() if x['platformId']}
check('18平台/20装备/10补给/32基础定义',len(platforms)==18 and sum(x['kind']!='Consumable' for x in items.values())==20 and sum(x['kind']=='Consumable' for x in items.values())==10 and len(items)+len(c['companions'])==32)
check('各目录ID无重复',all(unique(c[k]) for k in ['species','items','contracts','rareEvents','skills','services','companions','maps']))
check('全合同目标/昆虫外键有效',all(b['speciesId'] in species and b['count']>0 and x['mapId'] in species[b['speciesId']]['maps'] for x in c['contracts'] for b in x['targetBudget']) and all(i in species and i.startswith('I') for x in c['contracts'] for i in x['guaranteedInsects']))
check('全合同宿主与借用外键有效',all(x['mapId'] in maps and all(p in c['loanPackages'] for p in x['loans']) for x in c['contracts']))
loanrefs=[i for p in c['loanPackages'].values() for key in ['items','selectOne'] for i in p.get(key,[])]+[i for p in c['loanPackages'].values() for i in p.get('supplies',{})]
check('借用物品引用有效',all(i in items for i in loanrefs))
seen=set();remaining=set(contracts)
while remaining:
    eligible={i for i in remaining if contracts[i]['unlockAfter'] is None or contracts[i]['unlockAfter'] in seen}
    if not eligible:break
    seen|=eligible;remaining-=eligible
check('解锁依赖无环且全主线可达',not remaining,str(sorted(remaining)))
check('Q01五鼠一夜固定激活',sum(b['count'] for b in contracts['Q01']['targetBudget'] if b['role']=='Required')==5 and all(b['speciesId']=='M01' and b['activationSeconds']==0 for b in contracts['Q01']['targetBudget']) and contracts['Q01']['budgetRefill']==0)
check('L06三单猎杀无保护时钟',all(contracts[q]['type']=='Hunt' and contracts[q]['assetGroup'] is None and contracts[q]['durationSeconds'] is None and contracts[q]['mainPolicy']=='KillRequired' for q in ['Q16','Q17','Q18']))
check('保护时钟/60百分比一致',all(x['durationSeconds']>0 and x['protectionMinimum']==.6 and x['assetInitialUnits']>0 and x['acceptDriveOff'] for x in c['contracts'] if x['type']=='Protect'))
check('稀有12条与事件独立',len(rare)==12 and set(rare)=={s for s in species if s.startswith('C')})
check('C01无保底/结果持久与Q14鼹鼠保底',not rare['C01']['firstClueGuarantee'] and rare['C01']['probability']==.45 and rare['C01']['delaySecondsRange']==[180,300] and rare['C01']['persistDecision'] and rare['C05']['alwaysInQ14'])
coverage=[]
for sid,s in species.items():
    host=contracts.get(s['obtainContract'])
    if not host:coverage.append(sid+':宿主缺失');continue
    if sid.startswith('I'):
        if sid not in host['guaranteedInsects']:coverage.append(sid+':无标本保底')
    elif sid.startswith('C'):
        if sid not in rare:coverage.append(sid+':无稀有事件')
    else:
        rows=[b for b in host['targetBudget'] if b['speciesId']==sid]
        if not rows:coverage.append(sid+':宿主无目标')
        elif s['investigation']=='Corpse' and not any(b['role']!='Delivery' for b in rows):coverage.append(sid+':仅客户活交，尸体调查会违约')
check('45物种最低宿主与调查路径完整',not coverage,'; '.join(coverage))
check('普通/蛇/稀有调查模式',all(s['investigation']=='LiveContained' for s in c['species'] if s['id'].startswith('C') or s['id']=='S01') and all(s['investigation']=='Corpse' for s in c['species'] if s['id'].startswith('M') or s['id']=='S02'))
check('尸体四分之一/无昆虫货物',all(s['corpsePrice']==s['livePrice']//4 and s['donationXP']>=0 for s in c['species']) and all(s['livePrice']==s['corpsePrice']==s['donationXP']==0 for s in c['species'] if s['id'].startswith('I')))
check('26双级技能/48升级点/10终局格',len(c['skills'])==26 and all(len(s['rankFinalValues'])==2 for s in c['skills']) and sum(v['skillPointsGranted'] for v in c['levels'])==48 and c['player']['baseSlots']+sum(v['extraSlotsGranted'] for v in c['levels'])+4==10)
check('Lv1-25经验严格递增',len(c['levels'])==25 and all(a['totalXP']<b['totalXP'] for a,b in zip(c['levels'],c['levels'][1:])))
check('两类伙伴/免伤/最多2只',len(c['companions'])==2 and all(p['invulnerable'] and p['maximumOwned']==2 for p in c['companions']))
check('六反馈配置与随机答复一致',c['player']['crouchStepVolume']==c['player']['crouchStepStimulus']==0 and c['bait']['baseRadiusM']==9 and not c['bait']['automaticCaptureAtRadius'] and not c['interaction']['gestureThresholdCountsTowardsDuration'] and 'LoadSave' in c['layout']['keepOn'])
check('完整部署8/12/16与旧拥有不删',c['deployment']['baseTotal']==8 and c['deployment']['rankTotals']==[12,16] and c['deployment']['ownershipLimit'] is None)
newdocs=['完整白模开发总纲.md','V4反馈与交互界面规格.md','全地图白模规格.md','全委托与稀有事件规格.md','全生物白模与AI规格.md','全装备成长与经济规格.md','完整白模技术与存档规格.md','完整白模实施与验收.md']
check('当前8份规格存在且非空',all((ROOT/p).is_file() and (ROOT/p).stat().st_size>1500 for p in newdocs))
check('七张设计平面图存在',all((ROOT/'图示'/f'{i}_平面.png').is_file() for i in maps))
badlinks=[];linkcount=0
for p in ROOT.glob('*.md'):
    txt=p.read_text(encoding='utf-8')
    for link in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)',txt):
        if re.match(r'^(https?:|#|codex:)',link):continue
        dest=link.split('#',1)[0].strip('<>')
        if not dest:continue
        linkcount+=1
        if not (p.parent/dest).exists():badlinks.append(p.name+' → '+dest)
check('开发文档本地链接可解析',not badlinks,f'{linkcount}条；'+'; '.join(badlinks))
legacy=ROOT.parents[0]/'Backups'/'BeforeFullGameDocs-20261114'/'开发文档'
check('P0作者配置保留原字节',all((ROOT/n).read_bytes()==(legacy/n).read_bytes() for n in ['P0_Config_0.2.json','P0_Config_0.3.json']))

report=['# 完整白模文档校验报告','','日期：2026-11-14。范围：本地文档、作者JSON、ID/外键/获得路径/本地链接/平面图文件。**没有运行Unity、编译、自动游玩、键鼠整局或性能测试。**','','命令：`python G:/MyGame/开发文档/校验/validate_fullgame_docs.py`。脚本只读取内容并更新本报告；不操作游戏或存档。','',f'配置SHA-256：`{hashlib.sha256(CFG.read_bytes()).hexdigest()}`。','',f'结果：{sum(ok for _,ok,_ in checks)}/{len(checks)}项通过。','','| 检查 | 结果 | 说明 |','| --- | --- | --- |']
report += ['| '+name+' | '+('通过' if ok else '失败')+' | '+detail.replace('|','/').replace('\n',' ')+' |' for name,ok,detail in checks]
report += ['',f"合同单次基础奖励合计：{sum(x['rewardCash'] for x in c['contracts'])}元 / {sum(x['rewardXP'] for x in c['contracts'])}XP；54首次星额外1620元/2160XP。Lv25累计XP {c['levels'][-1]['totalXP']}；不假定一遍主线必须满级。",'','人工文档审查：六项反馈均有规格和验收；新单重抽/读档不变按用户明确答复；L06无畜栏/保护倒计时；高处/洞系有地面接手；Q09完整被偷物/Q13两虫/Q14鼹鼠/Q17相机机会保底。已视觉查看七张场地平面图，均标注设计而非游戏截图。','', '静态校验不能验证实际路径碰撞、AI手感、诱饵命中率、UI显示、音效、存档迁移或构建性能；上述须后续按FG01–FG20及18合同构建记录执行，当前均未新增通过声明。']
(ROOT/'校验'/'文档校验报告.md').write_text('\n'.join(report)+'\n',encoding='utf-8')
print(f'{sum(ok for _,ok,_ in checks)}/{len(checks)} passed')
for name,ok,detail in checks:
    if not ok:print('FAIL',name,detail)
sys.exit(0 if all(ok for _,ok,_ in checks) else 1)
