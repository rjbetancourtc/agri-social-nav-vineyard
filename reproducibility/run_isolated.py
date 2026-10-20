"""Audit all Python file reads during analysis; reject data outside this repository."""
from pathlib import Path
import sys,os,runpy,json,tempfile,platform
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1]
TEMP=ROOT/'_verification_tmp/temp'
TEMP.mkdir(parents=True,exist_ok=True)
tempfile.tempdir=str(TEMP)
os.environ['TEMP']=str(TEMP)
os.environ['TMP']=str(TEMP)
# Cache the installed runtime's OS identification before auditing scientific file reads.
# On Windows platform.machine() otherwise launches cmd/ver during a statsmodels import.
platform.uname()
RUNTIME=Path(sys.base_prefix).resolve()
reads=set();runtimeReads=0;rejected=[];blockedProcesses=[]
def audit(event,args):
 global runtimeReads
 if event=='subprocess.Popen':
  blockedProcesses.append(str(args[0]));raise PermissionError('External processes blocked during numerical verification')
 if event=='socket.connect':raise PermissionError('Network blocked during numerical verification')
 if event!='open' or not isinstance(args[0],(str,bytes,os.PathLike)):return
 p=Path(os.fsdecode(args[0])).resolve()
 if p.is_relative_to(ROOT):
  mode=args[1]
  if mode is None or not any(c in str(mode) for c in 'wax+'):reads.add(p.relative_to(ROOT).as_posix())
 elif p.is_relative_to(RUNTIME):
  runtimeReads+=1
 else:
  rejected.append(str(p));raise RuntimeError('Out-of-package file access rejected: '+str(p))
sys.addaudithook(audit)
os.chdir(ROOT)
dest=ROOT/'_verification_tmp'
dest.mkdir(exist_ok=True)
for script,args in [('reproduce.py',['--out',str(dest/'reproduced')]),('verify.py',['--results',str(dest/'reproduced')]),('check_tables.py',['--root',str(ROOT),'--out',str(dest/'checked')])]:
 sys.argv=[str(ROOT/'reproducibility'/script),*args]
 runpy.run_path(sys.argv[0],run_name='__main__')
(dest/'ISOLATION_AUDIT.json').write_text(json.dumps(dict(passed=True,packageReads=sorted(reads),installedRuntimeReads=runtimeReads,rejected=rejected,blockedProcessProbes=blockedProcesses,externalScientificInputs=False,networkOrSubprocessAllowed=False),indent=2),encoding='utf-8')
print('Isolated verification passed',flush=True)

