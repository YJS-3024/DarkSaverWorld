"""Keep native pixel textures lossless without replacing GUID/sprite/import metadata.
Unity serializes compression inside platformSettings, not TextureImporter root.
"""
import re

def default_platform(maximum=16384):
 return f'''  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {maximum}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
'''

def scalar(text,key,value,indent=2):
 prefix=' '*indent;pattern=r'^'+prefix+re.escape(key)+r':[^\n]*(?:\n|$)';line=f'{prefix}{key}: {value}\n'
 if re.search(pattern,text,re.M):return re.sub(pattern,lambda _:line,text,flags=re.M)
 return text+line

def normalize(text):
 if 'TextureImporter:\n'not in text:raise ValueError('Not a TextureImporter')
 if not re.search(r'^guid: [a-f0-9]{32}$',text,re.M):raise ValueError('Texture GUID missing')
 text=text.replace('\r\n','\n')
 # Drop the obsolete misplaced scalar; retaining it creates a false audit pass.
 text=re.sub(r'^  textureCompression:[^\n]*\n','',text,flags=re.M)
 text=re.sub(r'^(    filterMode:) \d+',r'\g<1> 0',text,flags=re.M)
 text=re.sub(r'^(    enableMipMap:) \d+',r'\g<1> 0',text,flags=re.M)
 text=scalar(text,'nPOTScale',0);text=scalar(text,'maxTextureSize',16384)
 match=re.search(r'^  platformSettings:\n(?:(?:  -|    )[^\n]*\n)*',text,re.M)
 blocks=[]
 if match:
  blocks=[part for part in re.split(r'(?=^  - serializedVersion:)',match[0].split('\n',1)[1],flags=re.M)if part.strip()]
 default=False;output=[]
 for block in blocks:
  target=re.search(r'^    buildTarget: (.+)$',block,re.M)
  if not target:raise ValueError('Platform setting has no buildTarget')
  default|=target[1]=='DefaultTexturePlatform'
  # Inactive platform overrides may be materialized by Unity later. Active/default
  # entries must retain native RGB/alpha and full source resolution.
  if target[1]!='DefaultTexturePlatform'and not re.search(r'^    overridden: 1$',block,re.M):output.append(block);continue
  for key,value in [('maxTextureSize',16384),('resizeAlgorithm',0),('textureFormat',-1),('textureCompression',0),('crunchedCompression',0)]:block=scalar(block,key,value,4)
  output.append(block)
 if not default:output.insert(0,default_platform())
 replacement='  platformSettings:\n'+''.join(output)
 if match:text=text[:match.start()]+replacement+text[match.end():]
 else:
  at=text.find('  userData:');at=at if at>=0 else len(text);text=text[:at]+replacement+text[at:]
 return text

def platforms(text):
 match=re.search(r'^  platformSettings:\n(?:(?:  -|    )[^\n]*\n)*',text,re.M)
 if not match:return []
 result=[]
 for block in re.split(r'(?=^  - serializedVersion:)',match[0].split('\n',1)[1],flags=re.M):
  row={key:value.strip()for key,value in re.findall(r'^    (\w+): ([^\n]*)$',block,re.M)}
  if row:result.append(row)
 return result

def lossless(text,width,height):
 """Inspect effective Unity platform settings; inactive override defaults are ignored."""
 def one(key,indent=2):
  match=re.search(r'^'+(' '*indent)+re.escape(key)+r': ([^\n]+)$',text,re.M);return match[1].strip()if match else None
 rows=platforms(text);default=next((row for row in rows if row.get('buildTarget')=='DefaultTexturePlatform'),None)
 if default is None or one('filterMode',4)!='0'or one('enableMipMap',4)!='0'or one('nPOTScale')!='0':return False
 effective=[default]+[row for row in rows if row is not default and row.get('overridden')=='1']
 return all(row.get('textureCompression')=='0'and row.get('textureFormat')in ['-1','4','5']and row.get('crunchedCompression','0')=='0'
            and row.get('maxTextureSize','').isdigit()and int(row['maxTextureSize'])>=max(width,height)for row in effective)
