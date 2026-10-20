from pathlib import Path
import hashlib
root=Path(__file__).resolve().parents[1]
for line in (root/'MANIFEST_SHA256.txt').read_text(encoding='utf-8').splitlines():
 h,name=line.split('  ',1)
 assert hashlib.sha256((root/name).read_bytes()).hexdigest()==h,name
print('SHA256 verified')
