"""Compare every numerical report baseline with a fresh independent-package analysis."""
from pathlib import Path
import sys,json,argparse
import numpy as np,pandas as pd
ROOT=Path(__file__).resolve().parents[1]
def compare(expected,actual,path=''):
 differences=[]
 if isinstance(expected,dict):
  for k,v in expected.items():
   if k in {'warnings','created','updated'}:continue
   if k not in actual:differences.append(path+'/'+k+' missing');continue
   differences+=compare(v,actual[k],path+'/'+k)
 elif isinstance(expected,list):
  if len(expected)!=len(actual):differences.append(path+' list length')
  else:
   for i,(a,b) in enumerate(zip(expected,actual)):differences+=compare(a,b,path+f'/{i}')
 elif isinstance(expected,(float,int)) and not isinstance(expected,bool):
  if not isinstance(actual,(float,int)) or not np.isclose(expected,actual,rtol=1e-7,atol=1e-9,equal_nan=True):differences.append(path+' numeric')
 elif expected!=actual:differences.append(path+' value')
 return differences
def verify(result):
 result=Path(result);checked=[];fail=[]
 for p in sorted((ROOT/'results/expected').iterdir()):
  q=result/p.name
  if not q.exists():fail.append(dict(file=p.name,reason='not regenerated'));continue
  if p.suffix=='.csv':
   left=pd.read_csv(p);right=pd.read_csv(q)
   if list(left.columns)!=list(right.columns) or left.shape!=right.shape:fail.append(dict(file=p.name,reason='shape/columns',expected=list(left.columns),actual=list(right.columns)));continue
   bad=[]
   for col in left:
    if pd.api.types.is_numeric_dtype(left[col]):
     try:
      if not np.allclose(left[col],right[col],rtol=1e-7,atol=1e-9,equal_nan=True):bad.append(col)
     except (TypeError,ValueError):bad.append(col)
    elif not left[col].fillna('<NA>').astype(str).equals(right[col].fillna('<NA>').astype(str)):bad.append(col)
   if bad:fail.append(dict(file=p.name,reason='values',columns=bad))
  else:
   diff=compare(json.loads(p.read_text(encoding='utf-8')),json.loads(q.read_text(encoding='utf-8')))
   if diff:fail.append(dict(file=p.name,reason='json differences',fields=diff))
  checked.append(p.name)
 benchmark=pd.read_csv(result/'BENCHMARK_REPRODUCED.csv')
 assert len(benchmark)==9 and benchmark.n.eq(20).all()
 record=dict(passed=not fail,compared=len(checked),files=checked,failures=fail,numericalTolerance=dict(rtol=1e-7,atol=1e-9),scope='Per-run metrics to complete reported statistical/decision analysis; no raw metric reconstruction; model warning wording is environment-dependent and excluded from numerical comparison.')
 (result/'COMPARISON_QA.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
 print(json.dumps(record),flush=True)
 if fail:raise SystemExit(1)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--results',type=Path,required=True);verify(p.parse_args().results)

