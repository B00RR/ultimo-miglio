# Icons for the HUD. Run: python3 build_icons.py  (needs cairosvg; Lucide SVGs in ./lucide, from npm lucide-static 1.57.0)
# Sources: Lucide (ISC, https://lucide.dev, LICENSE_Lucide_ISC.txt) unmodified paths, recoloured/stroke 2.2.
#          ORIGINAL (project-made, CC0): coin "L", bolt (nut), toll (barrier) - no Lucide equivalent for game currencies.
import cairosvg, re, os
here = os.path.dirname(os.path.abspath(__file__))
LUCIDE = {  # our name -> lucide name
 'home':'house','list':'list','truck':'truck','map':'map','bag':'shopping-bag','star':'star','drone':'drone',
 'gear':'settings','box':'package','down':'arrow-down-to-line','clock':'clock','phone':'smartphone','door':'door-open',
 'safe':'vault','cash':'banknote','chat':'message-circle','robot':'bot','lock':'lock','back':'chevron-left',
 'customer':'user','video':'video','coffee':'coffee','rocket':'rocket','arrow':'arrow-right',
}
ORIGINAL = {
 'coin':'<circle cx="12" cy="12" r="9"/><path d="M10 7v10h5"/>',
 'bolt':'<path d="M12 2l8.5 5v10L12 22l-8.5-5V7z"/><circle cx="12" cy="12" r="3.5"/>',
 'toll':'<path d="M4 21V9M4 9h16v4H4M9 9v4M14 9v4"/><circle cx="4" cy="6" r="2"/>',
}
def body_of(name):
    s = open(os.path.join(here, 'lucide', name + '.svg')).read()
    return re.search(r'<svg[^>]*>(.*)</svg>', s, re.S).group(1).strip()
P = {k: body_of(v) for k, v in LUCIDE.items()}; P.update(ORIGINAL)
os.makedirs(os.path.join(here, 'svg'), exist_ok=True)
for name, body in P.items():
    for suf, col in (('', '#3B2A20'), ('_cream', '#FFF9EE')):
        svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="{col}" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">{body}</svg>'
        if not suf: open(os.path.join(here, 'svg', name + '.svg'), 'w').write(svg)
        cairosvg.svg2png(bytestring=svg.encode(), write_to=os.path.join(here, '..', f'ic_{name}{suf}.png'), output_width=96, output_height=96)
print(len(P), 'icons:', len(LUCIDE), 'Lucide (ISC) +', len(ORIGINAL), 'original (CC0)')
