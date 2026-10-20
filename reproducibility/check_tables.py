"""Reproduce publication tables from packaged final CSVs, never run a mission.
Run: python -B reproducibility/reproduce_tables.py --root . --out reproduced_tables
"""
from pathlib import Path
import argparse,json
import numpy as np
import pandas as pd
from scipy import stats
def holm_adjust(values):
 values=np.asarray(values,dtype=float)
 order=np.argsort(values,kind="stable")
 adjusted=np.minimum(1,np.maximum.accumulate(values[order]*(len(values)-np.arange(len(values)))))
 result=np.empty_like(adjusted);result[order]=adjusted
 return result

def reproduce(root,out):
 root=Path(root);out=Path(out);out.mkdir(parents=True,exist_ok=True)
 D=pd.read_csv(root/'data/RUN_ENDPOINTS.csv');C=pd.read_csv(root/'results/expected/S1_CONTRASTES.csv')
 B=pd.read_csv(root/'data/benchmark/BENCHMARK_PER_ATTEMPT.csv')
 assert len(D)==360 and D.Identity.nunique()==360
 assert D.result.eq(2).sum()==266 and D.result.eq(3).sum()==94
 T=D.groupby(['Method','Scenario']).agg(planned=('Identity','size'),success=('result',lambda x:int(x.eq(2).sum())),timeout=('result',lambda x:int(x.eq(3).sum()))).reset_index()
 assert T.planned.eq(10).all()
 T.to_csv(out/'ACCOUNTING_REPRODUCED.csv',index=False)
 expected=pd.read_csv(root/'results/expected/ACCOUNTING_METHOD_SCENARIO.csv')
 assert np.array_equal(T.success,expected.sort_values(['Method','Scenario']).success)
 assert np.array_equal(T.timeout,expected.sort_values(['Method','Scenario']).terminalCode3)
 X=D.pivot_table(index='Method',columns='Scenario',values='result',aggfunc=lambda x:int(x.eq(2).sum()))
 X.to_csv(out/'TABLE3_SUCCESS.csv')
 S=B.groupby('Method').agg(n=('Identity','size'),Kernel=('Kernel_ms_mean','mean'),MeanP95=('Kernel_ms_p95','mean'),CommandAPI=('SensorToCommandAPI_ms_mean','mean'),Misses=('ComputeDeadlineMisses','sum'))
 assert len(B)==180 and B.Identity.nunique()==180 and S.n.eq(20).all()
 S.to_csv(out/'TABLE4_BENCHMARK.csv')
 for _,r in C.iterrows():
  x=D[(D.Method==4)&(D.Scenario==r.Scenario)][r.endpoint].dropna().to_numpy()
  y=D[(D.Method==r.Comparator)&(D.Scenario==r.Scenario)][r.endpoint].dropna().to_numpy()
  assert len(x)==r.n_M4 and len(y)==r.n_other
  if not len(x) or not len(y):
   assert pd.isna(r.estimate) and r.status.startswith('NOT_ESTIMABLE');continue
  assert np.isclose(x.mean()-y.mean(),r.estimate,atol=1e-9)
  if r.endpoint in ['Failure','IntimateEvent']:
   p=stats.fisher_exact([[int(x.sum()),len(x)-int(x.sum())],[int(y.sum()),len(y)-int(y.sum())]]).pvalue
   assert np.isclose(p,r.p_raw,atol=1e-12)
  else:
   if len(x)>1 and len(y)>1:
    test=stats.ttest_ind(x,y,equal_var=False)
    assert np.isclose(test.pvalue,r.p_raw,atol=1e-10)
    v1=x.var(ddof=1)/len(x);v2=y.var(ddof=1)/len(y);df=(v1+v2)**2/(v1*v1/(len(x)-1)+v2*v2/(len(y)-1))
    half=stats.t.ppf(.975,df)*np.sqrt(v1+v2)
    assert np.isclose(r.estimate-half,r.CI95_low,atol=1e-8) and np.isclose(r.estimate+half,r.CI95_high,atol=1e-8)
    pool=np.sqrt(((len(x)-1)*x.var(ddof=1)+(len(y)-1)*y.var(ddof=1))/(len(x)+len(y)-2))
    effect=(x.mean()-y.mean())/pool*(1-3/(4*(len(x)+len(y))-9))
    assert np.isclose(effect,r.effect,atol=1e-8)
 for _,g in C.groupby('family'):
  expectedp=holm_adjust(g.p_raw.fillna(1))
  assert np.allclose(g.p_Holm.dropna(),expectedp[g.p_Holm.notna()],atol=1e-12)
 C.to_csv(out/'S1_CONTRASTS_CHECKED.csv',index=False)
 for s in [1,2,3,4,'POOLED']:
  g=D if s=='POOLED' else D[D.Scenario==s]
  keys=['MissionUtility','SafetyUtility','PersonalExposureFraction']
  assert g[keys].notna().all().all()
  calc=g.groupby('Method')[keys].mean().reindex(range(9))
  expectedm=pd.read_csv(root/f'results/expected/{s}_DECISION_ALL_CRITERIA.csv',index_col=0)
  assert np.allclose(calc,expectedm,atol=1e-12)
  calc.to_csv(out/f'{s}_DECISION_REPRODUCED.csv')
  X=calc.to_numpy()*np.array([1,1,-1]);tol=1e-5
  dom=np.array([[bool(np.all(a>=b-tol) and np.any(a>b+tol)) for b in X] for a in X])
  expectedDom=pd.read_csv(root/f'results/expected/{s}_DOMINANCE.csv',index_col=0).to_numpy()
  assert np.array_equal(dom,expectedDom)
  pd.DataFrame(dom.astype(int),index=range(9),columns=range(9)).to_csv(out/f'{s}_DOMINANCE_REPRODUCED.csv')
  norm=np.sqrt((calc.to_numpy()**2).sum(axis=0));Z=calc.to_numpy()/norm/3
  plus=np.array([Z[:,0].max(),Z[:,1].max(),Z[:,2].min()]);minus=np.array([Z[:,0].min(),Z[:,1].min(),Z[:,2].max()])
  dp=np.linalg.norm(Z-plus,axis=1);dm=np.linalg.norm(Z-minus,axis=1);cc=dm/(dp+dm)
  published=pd.read_csv(root/f'results/expected/{s}_TOPSIS_equal.csv').coefficient
  assert np.allclose(cc,published,atol=1e-12)
 report=dict(passed=True,planned=360,success=266,timeout=94,contrasts=128,benchmark=180,independentTestsVerified=True,continuousIntervalsAndEffectsVerified=True,HolmVerified=True,decisionMatricesVerified=5,dominanceVerified=5,equalTOPSISVerified=5,notRerun='This independent checker does not regenerate models or random draws; reproduce.py does. No acquisition, recovery or Unity simulation is executed.')
 (out/'TABLE_REPRODUCTION_QA.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report),flush=True)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--root',required=True);p.add_argument('--out',required=True);a=p.parse_args();reproduce(a.root,a.out)
