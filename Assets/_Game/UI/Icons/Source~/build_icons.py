# Line icons (Lucide-style, 24 grid). Lifted from mockups/icons.py + extras. Run: python3 build_icons.py
import cairosvg, os
I = {}
exec(open(os.path.join(os.path.dirname(__file__), '../../../../../../mockups/icons.py')).read().split('def ic')[0], I)
P = dict(I['I'])
P.update({
 'phone':'<rect x="6" y="2" width="12" height="20" rx="2.5"/><path d="M11 18h2"/>',
 'door':'<path d="M5 21V4a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1v17M3 21h18"/><circle cx="15" cy="12" r="1"/>',
 'toll':'<path d="M4 21V9M4 9h16v4H4M9 9v4M14 9v4"/><circle cx="4" cy="6" r="2"/>',
 'safe':'<rect x="3" y="4" width="18" height="16" rx="2"/><circle cx="12" cy="12" r="4"/><path d="M12 8v1M16 12h-1M7 20v2M17 20v2"/>',
 'cash':'<rect x="2" y="6" width="20" height="12" rx="2"/><circle cx="12" cy="12" r="3"/>',
 'chat':'<path d="M21 12a8 8 0 0 1-11.6 7.1L3 21l1.9-6.4A8 8 0 1 1 21 12z"/>',
 'robot':'<rect x="5" y="8" width="14" height="11" rx="3"/><path d="M12 4v4M9 13h.01M15 13h.01M9 16h6"/>',
 'lock':'<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>',
 'back':'<path d="M15 5l-7 7 7 7"/>',
 'customer':'<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
 'video':'<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M10 9l5 3-5 3z"/>',
 'coffee':'<path d="M4 9h13v5a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5zM17 10h1.5a2.5 2.5 0 0 1 0 5H17M8 3v3M12 3v3"/>',
 'rocket':'<path d="M12 2c4 3 5 8 3 13H9C7 10 8 5 12 2zM9 15l-3 4h4M15 15l3 4h-4"/><circle cx="12" cy="9" r="1.5"/>',
 'arrow':'<path d="M5 12h14M13 6l6 6-6 6"/>',
})
for name, body in P.items():
    for suf, col in (('', '#3B2A20'), ('_cream', '#FFF9EE')):
        svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="{col}" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">{body}</svg>'
        if not suf: open(f'svg/{name}.svg','w').write(svg)
        cairosvg.svg2png(bytestring=svg.encode(), write_to=f'../ic_{name}{suf}.png', output_width=96, output_height=96)
print(len(P), 'icons')
