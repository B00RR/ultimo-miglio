import re,sys,os,xml.etree.ElementTree as ET
R='/workspace/ui-clone-pr36/'
def uss(path):
    s=open(R+path).read()
    s=re.sub(r'url\("project://database/([^"?]+)[^"]*"\)',lambda m:'url("file://'+R+m.group(1)+'")',s)
    s=re.sub(r'-unity-font-definition:[^;]*;','',s)
    s=s.replace('-unity-text-align: middle-center','text-align:center;align-items:center;justify-content:center')
    s=s.replace(':root',':root')
    return s
def node(e,force_show):
    tag=e.tag.split('}')[-1]
    if tag=='Style':return ''
    a=e.attrib; cls=a.get('class',''); nm=a.get('name',''); st=a.get('style','')
    if force_show and nm in force_show: st=st.replace('display: none;','')
    kids=''.join(node(c,force_show) for c in e)
    t=a.get('text','')
    if tag=='Slider':
        return f'<input type="range" id="{nm}" class="{cls}" min="0" max="1" step="0.01" value="{a.get("value","0")}" style="{st}">'
    h={'Label':'span','Button':'button'}.get(tag,'div')
    if tag=='UXML':return kids
    return f'<{h} id="{nm}" class="ve {cls} {"lbl" if h!="div" else ""}" style="{st}">{t}{kids}</{h}>'
def build(uxml,styles,show,extra,out):
    root=ET.parse(R+uxml).getroot()
    css=''.join(uss(p) for p in styles)
    font=R+'Assets/_Game/UI/Fonts/Fredoka-Bold.ttf'
    html=f'''<html><head><style>@font-face{{font-family:F;src:url("file://{font}")}}
html,body{{margin:0;width:390px;height:844px;overflow:hidden;font-family:F,sans-serif}}
body{{background:linear-gradient(#cfe7e3,#e9d3a8)}}
.ve{{display:flex;flex-direction:column;position:relative;box-sizing:border-box;border-style:solid;border-width:0}}
span.ve,button.ve{{display:flex;white-space:nowrap}}
.fill,.layer{{position:absolute;left:0;top:0;right:0;bottom:0}}
.icon{{background-size:contain;background-repeat:no-repeat;background-position:center}}
{css}{extra}</style></head><body><div class="ve fill um-portrait">{node(root,show)}</div></body></html>'''
    open(out,'w').write(html)
T=['Assets/_Game/UI/Tokens.uss','Assets/_Game/UI/USS/DioramaSolare.uss','Assets/_Game/UI/USS/HudV2.uss','Assets/_Game/UI/USS/Settings.uss']
build('Assets/_Game/UI/UXML/Hud.uxml',T,set(),'','hud.html')
build('Assets/_Game/UI/UXML/Hud.uxml',T,{'settings-root'},'#zoom-value::after{content:""}','settings.html')
