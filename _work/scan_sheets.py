import re, glob, os
os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'sheets'))
for f in sorted(glob.glob('*.md')):
    if f == 'README.md':
        continue
    t = open(f, encoding='utf-8').read()
    m = re.search(r'```\n(.*?)```', t, re.S)
    grid = m.group(1).strip('\n').split('\n')
    top = grid[0] + grid[1]
    xs = sorted(set([(i % 20) * 10 + 5 for i, ch in enumerate(top) if ch in '=:']))
    rivers = sorted(set([(i % 20) * 10 + 5 for i, ch in enumerate(top) if ch == 'R']))
    print(f, 'entry=', xs, 'river_top=', rivers)
    print('   row0=', grid[0])
