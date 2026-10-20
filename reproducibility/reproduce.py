"""Recompute current analysis from packaged per-run metrics only."""
from pathlib import Path
import sys,argparse,json
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'code'))
from scientific_methods import *
def reproduce(out):
 out=Path(out).resolve();out.mkdir(parents=True,exist_ok=True)
 spec=json.loads((ROOT/'config/analysis.json').read_text(encoding='utf-8'))
 supplied=pd.read_csv(ROOT/'data/RUN_ENDPOINTS.csv')
 data=utilities(supplied)
 for name in ['Failure','IntimateEvent','DurationCompleted_s',*CRITERIA]:
  assert np.allclose(data[name],supplied[name],rtol=1e-12,atol=1e-12,equal_nan=True),name
 assert len(data)==360 and data.Identity.nunique()==360
 assert data.result.eq(2).sum()==266 and data.result.eq(3).sum()==94
 data['Block']=data.Scenario.astype(str)+'_'+data.Seed.astype(str)
 paired=False;rng=np.random.default_rng(spec['analysisSeed']);account=[];binary=[];descriptive=[]
 for (m,s),g in data.groupby(['Method','Scenario']):
  technical=int(g.TechnicalMissing.sum());n=len(g);success=int(g.result.eq(2).sum());row=dict(Method=m,Scenario=s,planned=n,committed=n-technical,technical=technical,success=success,successLowerBound=success/n,successUpperBound=(success+technical)/n)
  for code in range(2,7):row['terminalCode'+str(code)]=int(g.result.eq(code).sum())
  account.append(row)
  for ep in ['Failure','IntimateEvent','FailureIncludingTechnicalSensitivity']:
   observed=g[ep].dropna();k=int(observed.sum());no=len(observed);lo,hi=exact_interval(k,no);binary.append(dict(Method=m,Scenario=s,endpoint=ep,planned=n,observed=no,unknown=n-no,count=k,estimate=k/no if no else None,CI95_low=lo,CI95_high=hi,plannedRiskLower=k/n,plannedRiskUpper=(k+n-no)/n,interval='Clopper-Pearson 95% observed denominator; planned-risk identification bounds separate'))
  for ep in ['DurationCompleted_s','LengthXZ_m','Exposure045_s','Exposure12_s','Exposure36_s','PersonalExposureFraction','MinGroundTruthXZ_m','MaxRealized_mps','StopTime005_s']:
   a=g[ep].dropna().to_numpy(float);q=dict(Method=m,Scenario=s,endpoint=ep,n=len(a),mean=float(a.mean()) if len(a) else None,SD=float(a.std(ddof=1)) if len(a)>1 else None,median=float(np.median(a)) if len(a) else None,CI95_low=None,CI95_high=None,estimand='conditional on mission success' if ep=='DurationCompleted_s' else 'observed RUN-END, including failed missions; exposure duration differs by termination')
   if len(a)>1:
    h=stats.t.ppf(.975,len(a)-1)*a.std(ddof=1)/np.sqrt(len(a));q.update(CI95_low=float(a.mean()-h),CI95_high=float(a.mean()+h))
   descriptive.append(q)
 pd.DataFrame(account).to_csv(out/'ACCOUNTING_METHOD_SCENARIO.csv',index=False);pd.DataFrame(binary).to_csv(out/'BINARY_ENDPOINTS_EXACT_CI.csv',index=False);pd.DataFrame(descriptive).to_csv(out/'DESCRIPTIVES.csv',index=False)
 contrasts=[]
 for ep in spec['primaryEndpoints']+spec['secondaryContrastEndpoints']:
  print('Recalculating',ep,flush=True);factorial(data,ep,out,paired)
  for s in range(1,5):
   for m in spec['comparators']:
    a=data[(data.Method==4)&(data.Scenario==s)];b=data[(data.Method==m)&(data.Scenario==s)];r=contrast(a,b,ep,paired,rng,spec['bootstrapReplicates'])
    if ep in spec['primaryEndpoints']:r['effect_CI95_low']=r['CI95_low'];r['effect_CI95_high']=r['CI95_high']
    elif not paired:
     x=a[ep].dropna().to_numpy();y=b[ep].dropna().to_numpy();effects=[]
     if len(x)>1 and len(y)>1:
      for _ in range(spec['bootstrapReplicates']):
       xx=rng.choice(x,len(x));yy=rng.choice(y,len(y));pool=np.sqrt(((len(xx)-1)*xx.var(ddof=1)+(len(yy)-1)*yy.var(ddof=1))/(len(xx)+len(yy)-2))
       if pool>1e-12:effects.append((xx.mean()-yy.mean())/pool*(1-3/(4*(len(xx)+len(yy))-9)))
      if len(effects)>=spec['bootstrapReplicates']//2:r.update(effect_CI95_low=float(np.quantile(effects,.025)),effect_CI95_high=float(np.quantile(effects,.975)))
    r.update(endpoint=ep,Scenario=s,Comparator=m,family='primary' if ep in spec['primaryEndpoints'] else 'secondary');contrasts.append(r)
 T=pd.DataFrame(contrasts)
 for _,idx in T.groupby('family').groups.items():
  p=T.loc[idx,'p_raw'];T.loc[idx,'p_Holm']=np.where(p.notna(),multipletests(p.fillna(1),method='holm')[1],np.nan)
 T.to_csv(out/'S1_CONTRASTES.csv',index=False);print('Recalculating ranking sets and resampling',flush=True);rankings(data,out,rng,spec,paired)

 for scenario in [1,2,3,4,'POOLED']:
  dm=pd.read_csv(out/f'{scenario}_DECISION_ALL_CRITERIA.csv',index_col=0)
  dm.corr(method='spearman').to_csv(out/f'{scenario}_CORRELATIONS_METHOD_MEANS.csv')
  w=pd.read_csv(out/f'{scenario}_WEIGHT_SENSITIVITY.csv')
  w.assign(top1=w['rank'].eq(1)).groupby('Method').agg(draws=('draw','size'),top1Fraction=('top1','mean')).to_csv(out/f'{scenario}_WEIGHT_SUMMARY.csv')
 sensitivity=[]
 for scenario in [1,2,3,4,'POOLED']:
  dm=pd.read_csv(out/f'{scenario}_DECISION_ALL_CRITERIA.csv',index_col=0)
  for profile,cols,benefits in [('MissionClearance',[0,1],[True,True]),('MissionExposure',[0,2],[True,False])]:
   r=topsis(dm.to_numpy()[:,cols],benefits,[.5,.5])
   for m,cc in enumerate(r['coefficient']):sensitivity.append(dict(Set=scenario,Profile=profile,Method=m,coefficient=cc,rank=stats.rankdata(-r['coefficient'],method='min')[m]))
 pd.DataFrame(sensitivity).to_csv(out/'TWO_CRITERION_SENSITIVITY.csv',index=False)
 bench=pd.read_csv(ROOT/'data/benchmark/BENCHMARK_PER_ATTEMPT.csv')
 assert len(bench)==180 and bench.Identity.nunique()==180 and set(bench.Identity)<=set(data.Identity)
 bench.groupby('Method').agg(n=('Identity','size'),Kernel=('Kernel_ms_mean','mean'),MeanP95=('Kernel_ms_p95','mean'),CommandAPI=('SensorToCommandAPI_ms_mean','mean'),Misses=('ComputeDeadlineMisses','sum')).to_csv(out/'BENCHMARK_REPRODUCED.csv')
 quality=pd.read_csv(ROOT/'data/QUALITY_PER_RUN.csv')
 assert set(quality.Identity)==set(data.Identity) and len(quality)==360
 assert int(quality.OriginalStrictQualityFlag.sum())==115
 plan=json.loads((ROOT/'config/RUN_PLAN.json').read_text(encoding='utf-8'))['runs']
 expected={f"EVAL_20261019_112257_M{x['method']}_E{x['scenario']}_R{x['run']}_S{x['seed']}_A{x['attempt']}" for x in plan}
 assert expected==set(data.Identity)
 print('Analysis complete',flush=True)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--out',type=Path,default=ROOT/'reproduced');a=p.parse_args();reproduce(a.out)
