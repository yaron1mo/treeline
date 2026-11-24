import csv, math
TYPES=["recruit","regular","special_forces","motorbike","buggy","van","mrap","ifv","old_tank","modern_tank","helicopter"]
CLASS={"machine_gun":0,"anti_tank":1,"air_defence":2}
CS=["mg","at","ad"]
def load(probe, wc):
    waves={}; tag={}
    for r in csv.DictReader(open(probe)):
        w=int(r['wave']); waves.setdefault(w,{}).setdefault(r['unit'],0); waves[w][r['unit']]+=int(r['count'])
        t=r['notes']; tag[w]= t if not t.startswith(('single','pair','triple')) else t.split(':')[0]
    cost={}
    for r in csv.DictReader(open(wc)):
        try: v=float(r['cost'])
        except: v=None
        if v is not None and math.isnan(v): v=None
        cost[(r['field'],int(r['wave']))]=v
    return waves, tag, cost

def interp(pts, n):
    pts=sorted((x,c) for x,c in pts if c is not None)
    if not pts: return None
    if n<=pts[0][0]: return pts[0][1]*n/pts[0][0] if pts[0][0] else pts[0][1]
    for (x0,c0),(x1,c1) in zip(pts,pts[1:]):
        if x0<=n<=x1: return c0+(c1-c0)*(n-x0)/(x1-x0)
    (x0,c0),(x1,c1)=pts[-2],pts[-1]
    return c1+(c1-c0)/(x1-x0)*(n-x1)

def curves(waves,tag,cost,fields):
    cv={}
    for w,comp in waves.items():
        if tag[w].startswith('single:'):
            u=list(comp)[0]
            for f in fields:
                c=cost.get((f,w))
                cv.setdefault((f,u),[]).append((sum(comp.values()),c))
    return cv
