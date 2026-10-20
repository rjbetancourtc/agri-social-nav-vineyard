"""Effective numerical functions; original raw-acquisition entry points excluded."""
import pathlib,json,itertools,warnings,sys,datetime
import numpy as np
import pandas as pd
from scipy import stats
from statsmodels.stats.multitest import multipletests

def write(path,value):
 pathlib.Path(path).write_text(json.dumps(finite_json(value),indent=2,allow_nan=False),encoding="utf-8")
def _decision_inputs(matrix,benefit):
 a=np.asarray(matrix,float);b=np.asarray(benefit)
 if a.ndim!=2 or a.shape[0]<2 or a.shape[1]<1 or not np.isfinite(a).all():raise ValueError('finite 2D decision matrix with at least two alternatives required')
 if b.shape!=(a.shape[1],) or not np.isin(b,[False,True]).all():raise ValueError('one boolean cost/benefit flag per criterion required')
 return a,b.astype(bool)

def topsis(matrix,benefit,weights):
 a,benefit=_decision_inputs(matrix,benefit);w=np.asarray(weights,float)
 if w.shape!=(a.shape[1],) or not np.isfinite(w).all() or np.any(w<0):raise ValueError('finite nonnegative weight per criterion required')
 # Constant criteria and zero-weight criteria cannot discriminate alternatives.
 # Reject the degenerate case before normalization instead of emitting NaN ranks.
 keep=(np.ptp(a,axis=0)>1e-12)&(w>0);a=a[:,keep];w=w[keep];benefit=benefit[keep]
 if not a.shape[1]:raise ValueError('all criteria constant')
 w=w/w.sum();norm=np.sqrt((a*a).sum(axis=0));z=a/norm*w
 plus=np.where(benefit,z.max(axis=0),z.min(axis=0));minus=np.where(benefit,z.min(axis=0),z.max(axis=0));dp=np.linalg.norm(z-plus,axis=1);dm=np.linalg.norm(z-minus,axis=1);cc=dm/(dp+dm)
 return dict(keep=keep,weights=w,normalizedWeighted=z,positive=plus,negative=minus,dPositive=dp,dNegative=dm,coefficient=cc)

def dominance(matrix,benefit,tol=1e-10):
 a,benefit=_decision_inputs(matrix,benefit)
 if not np.isscalar(tol) or not np.isfinite(tol) or tol<0:raise ValueError('finite nonnegative dominance tolerance required')
 a=a*np.where(benefit,1,-1);out=np.zeros((len(a),len(a)),int)
 for i,j in itertools.permutations(range(len(a)),2):out[i,j]=int(np.all(a[i]>=a[j]-tol) and np.any(a[i]>a[j]+tol))
 return out

VERSION='2.0.0'

CRITERIA=['MissionUtility','SafetyUtility','PersonalExposureFraction']

BENEFIT=[True,True,False]

PROFILES={'equal':[1/3]*3,'group_balanced':[.5,.25,.25],'mission_priority':[.7,.15,.15],'safety_priority':[.2,.4,.4]}

def finite_json(value):
 if isinstance(value,dict):return {k:finite_json(v) for k,v in value.items()}
 if isinstance(value,list):return [finite_json(v) for v in value]
 if isinstance(value,(float,np.floating)) and not np.isfinite(value):return None
 return value

def utilities(data):
 d=data.copy();d['Failure']=(d.result!=2).astype(float);d['IntimateEvent']=np.where(d.MinGroundTruthXZ_m.notna(),(d.MinGroundTruthXZ_m<.45).astype(float),np.nan)
 d['DurationCompleted_s']=d.Duration_s.where(d.result==2)
 d['MissionUtility']=np.where(d.result==2,np.maximum(0,1-d.Duration_s/180),0)
 d['SafetyUtility']=np.clip((d.MinGroundTruthXZ_m-.45)/(1.2-.45),0,1)
 known=d.Duration_s-d.MissingGroundTruth_s;d['PersonalExposureFraction']=np.where(known>0,d.Exposure12_s/known,np.nan)
 return d

def paired_bootstrap(data,rng):
 groups=list(data.groupby('Block',sort=True));chosen=rng.integers(0,len(groups),len(groups));pieces=[]
 for i,j in enumerate(chosen):
  g=groups[j][1].copy();g['BootstrapBlock']=i;pieces.append(g)
 return pd.concat(pieces,ignore_index=True)

def contrast(left,right,endpoint,paired,rng,nboot=2000):
 a=left[['Block',endpoint]].dropna();b=right[['Block',endpoint]].dropna();binary=endpoint in ['Failure','IntimateEvent'];result=dict(n_M4=len(a),n_other=len(b),n_pairs=None,estimate=None,CI95_low=None,CI95_high=None,p_raw=None,effect=None,effect_type='risk_difference' if binary else 'paired_dz' if paired else 'independent_Hedges_g',effect_CI95_low=None,effect_CI95_high=None,estimand='joint-completer paired mean difference' if endpoint=='DurationCompleted_s' and paired else 'M4 minus comparator over observed endpoint values')
 if paired:
  j=a.merge(b,on='Block',suffixes=('_a','_b'),validate='one_to_one');x=j[endpoint+'_a'].to_numpy();y=j[endpoint+'_b'].to_numpy();n=len(x);result['n_pairs']=n
 else:x=a[endpoint].to_numpy();y=b[endpoint].to_numpy();n=min(len(x),len(y))
 if not len(x) or not len(y):return dict(**result,status='NOT_ESTIMABLE_EMPTY_CELL')
 estimate=float(x.mean()-y.mean());result['estimate']=estimate
 if binary:
  if paired:
   discordA=int(((x==1)&(y==0)).sum());discordB=int(((x==0)&(y==1)).sum());p=stats.binomtest(discordA,discordA+discordB,.5).pvalue if discordA+discordB else 1.
  else:p=stats.fisher_exact([[int(x.sum()),len(x)-int(x.sum())],[int(y.sum()),len(y)-int(y.sum())]]).pvalue
  boot=[]
  for _ in range(nboot):
   if paired:idx=rng.integers(0,n,n);boot.append(np.mean(x[idx]-y[idx]))
   else:boot.append(rng.choice(x,len(x)).mean()-rng.choice(y,len(y)).mean())
  result.update(p_raw=float(p),effect=estimate,CI95_low=float(np.quantile(boot,.025)),CI95_high=float(np.quantile(boot,.975)),status='ESTIMATED');return result
 if n<2:return dict(**result,status='NOT_ESTIMABLE_N_LT_2')
 if paired:
  diff=x-y;sd=float(diff.std(ddof=1));se=sd/np.sqrt(n);df=n-1;effect=float(diff.mean()/sd) if sd>1e-12 else None
  p=float(stats.ttest_1samp(diff,0).pvalue) if sd>1e-12 else 1. if abs(estimate)<1e-12 else None
 else:
  v1=x.var(ddof=1)/len(x);v2=y.var(ddof=1)/len(y);se=np.sqrt(v1+v2);df=(v1+v2)**2/(v1*v1/(len(x)-1)+v2*v2/(len(y)-1)) if v1+v2>0 else np.nan
  pool=np.sqrt(((len(x)-1)*x.var(ddof=1)+(len(y)-1)*y.var(ddof=1))/(len(x)+len(y)-2));effect=estimate/pool*(1-3/(4*(len(x)+len(y))-9)) if pool>1e-12 else None;p=float(stats.ttest_ind(x,y,equal_var=False).pvalue) if se>0 else None
 q=stats.t.ppf(.975,df) if np.isfinite(df) else np.nan;result.update(p_raw=p,effect=effect,CI95_low=float(estimate-q*se),CI95_high=float(estimate+q*se),status='ESTIMATED' if p is not None else 'DEGENERATE_VARIANCE')
 if paired:
  effects=[]
  for _ in range(nboot):
   z=(x-y)[rng.integers(0,n,n)];std=z.std(ddof=1)
   if std>1e-12:effects.append(z.mean()/std)
  if len(effects)>=nboot//2:result.update(effect_CI95_low=float(np.quantile(effects,.025)),effect_CI95_high=float(np.quantile(effects,.975)))
 return result

def factorial(data,endpoint,out,paired):
 import statsmodels.formula.api as smf
 from statsmodels.stats.diagnostic import het_breuschpagan
 d=data[data[endpoint].notna()].copy();d['value']=d[endpoint];coverage=d.groupby(['Method','Scenario']).size();diagnostic=dict(endpoint=endpoint,n=len(d),cells=len(coverage),expectedCells=36,formula='value ~ C(Method,Sum)*C(Scenario,Sum)',warnings=[])
 if len(coverage)!=36:
  diagnostic['status']='NOT_ESTIMABLE_FULL_FACTORIAL_MISSING_CELLS';(out/(endpoint+'_FACTORIAL.json')).write_text(json.dumps(diagnostic,indent=2));return
 fit=None
 with warnings.catch_warnings(record=True) as caught:
  warnings.simplefilter('always')
  if paired and endpoint not in ['Failure','IntimateEvent']:
   try:
    mixed=smf.mixedlm(diagnostic['formula'],d,groups=d.Block).fit(reml=True,method='lbfgs');diagnostic['mixedConverged']=bool(mixed.converged);diagnostic['randomVariance']=float(np.asarray(mixed.cov_re).ravel()[0]);(out/(endpoint+'_MIXED_MODEL.txt')).write_text(str(mixed.summary()))
    if mixed.converged and diagnostic['randomVariance']>1e-8:fit=mixed;diagnostic['model']='mixed random block intercept'
   except Exception as e:diagnostic['mixedFailure']=str(e)
  robust=smf.ols(diagnostic['formula'],d).fit(cov_type='cluster',cov_kwds={'groups':d.Block},use_t=True) if paired else smf.ols(diagnostic['formula'],d).fit(cov_type='HC3',use_t=True)
  if fit is None:fit=robust;diagnostic['model']=('cluster robust' if paired else 'independent HC3')+' sum-coded saturated OLS; linear probability if binary'
  fixedCov=fit.cov_params().loc[fit.fe_params.index,fit.fe_params.index] if hasattr(fit,'fe_params') else fit.cov_params();diagnostic['designRank']=int(np.linalg.matrix_rank(fit.model.exog));diagnostic['covarianceRank']=int(np.linalg.matrix_rank(fixedCov));diagnostic['parameterCount']=int(fit.model.exog.shape[1]);diagnostic['clusters']=int(d.Block.nunique())
  try:fit.wald_test_terms(skip_single=False,scalar=True).table.to_csv(out/(endpoint+'_FACTORIAL_TESTS.csv'))
  except Exception as e:diagnostic['waldFailure']=str(e)
  residual=np.asarray(fit.resid);None
  diagnostic['shapiroResidual_p']=float(stats.shapiro(residual).pvalue) if residual.std()>1e-12 else None
  try:diagnostic['breuschPagan_p']=float(het_breuschpagan(residual,fit.model.exog)[1])
  except Exception:diagnostic['breuschPagan_p']=None
  groups=[g[endpoint].to_numpy() for _,g in d.groupby(['Method','Scenario'])];diagnostic['brownForsythe_p']=float(stats.levene(*groups,center='median').pvalue)
  diagnostic['warnings']=[str(w.message) for w in caught]
 diagnostic['status']='ESTIMATED_WITH_DIAGNOSTICS';(out/(endpoint+'_FACTORIAL.json')).write_text(json.dumps(finite_json(diagnostic),indent=2,allow_nan=False))

def exact_interval(k,n):
 if n==0:return [None,None]
 return [float(stats.beta.ppf(.025,k,n-k+1)) if k else 0.,float(stats.beta.ppf(.975,k+1,n-k)) if k<n else 1.]

def resample(group,rng,paired):
 if paired:return pd.concat([paired_bootstrap(g,rng) for _,g in group.groupby('Scenario')],ignore_index=True)
 return pd.concat([g.iloc[rng.integers(0,len(g),len(g))] for _,g in group.groupby(['Scenario','Method'])],ignore_index=True)

def rankings(data,out,rng,spec,paired):
 statuses=[];bootN=spec['rankingBootstrapReplicates'];samples=spec['weightSensitivityDraws']
 for s,g in [(s,data[data.Scenario==s]) for s in range(1,5)]+[('POOLED',data)]:
  matrix=g.groupby('Method')[CRITERIA].mean().reindex(range(9));matrix.to_csv(out/f'{s}_DECISION_ALL_CRITERIA.csv');g[CRITERIA].corr(method='spearman').to_csv(out/f'{s}_CORRELATIONS.csv')
  # Never hide missing attempts by retaining only surviving methods.
  if g.TechnicalMissing.any() or not np.isfinite(g[CRITERIA]).all().all() or not np.isfinite(matrix).all().all():
   statuses.append(dict(Scenario=s,status='NOT_ESTIMABLE_MISSING_CRITERIA_OR_TECHNICAL_ATTEMPT',n=int(len(g)),technical=int(g.TechnicalMissing.sum())));continue
  X=matrix.to_numpy();dom=dominance(X,BENEFIT,spec['paretoTolerance']);pd.DataFrame(dom,index=matrix.index,columns=matrix.index).to_csv(out/f'{s}_DOMINANCE.csv');pd.DataFrame(dict(Method=matrix.index,nonDominated=dom.sum(axis=0)==0)).to_csv(out/f'{s}_PARETO_SET.csv',index=False)
  equal=None
  for name,weights in PROFILES.items():
   try:r=topsis(X,BENEFIT,weights)
   except ValueError as e:
    write(out/f'{s}_TOPSIS_{name}_NOT_ESTIMABLE.json',dict(reason=str(e)));continue
   pd.DataFrame(dict(Method=matrix.index,coefficient=r['coefficient'],rank=stats.rankdata(-r['coefficient'],method='min'),dPositive=r['dPositive'],dNegative=r['dNegative'])).to_csv(out/f'{s}_TOPSIS_{name}.csv',index=False)
   write(out/f'{s}_TOPSIS_{name}_DETAIL.json',{k:np.asarray(v).tolist() for k,v in r.items()})
   if name=='equal':equal=r
  weightRows=[];bootRows=[];dominanceCounts=np.zeros((9,9));skipped=0
  for draw in range(samples):
   mission=rng.uniform(.1,.9);split=rng.uniform(.1,.9);weights=[mission,(1-mission)*split,(1-mission)*(1-split)]
   try:r=topsis(X,BENEFIT,weights)
   except ValueError:continue
   ranks=stats.rankdata(-r['coefficient'],method='min')
   for m,cc in enumerate(r['coefficient']):weightRows.append(dict(draw=draw,Method=m,wMission=weights[0],wDistance=weights[1],wExposure=weights[2],coefficient=cc,rank=ranks[m]))
  for b in range(bootN):
   M=resample(g,rng,paired).groupby('Method')[CRITERIA].mean().reindex(range(9)).to_numpy();D=dominance(M,BENEFIT,spec['paretoTolerance']);dominanceCounts+=D
   try:r=topsis(M,BENEFIT,PROFILES['equal']);ranks=stats.rankdata(-r['coefficient'],method='min');ties=int((ranks==1).sum())
   except ValueError:skipped+=1;continue
   for m in range(9):bootRows.append(dict(bootstrap=b,Method=m,coefficient=r['coefficient'][m],rank=ranks[m],fractionalTop1=1/ties if ranks[m]==1 else 0,nonDominated=int(D[:,m].sum()==0)))
  W=pd.DataFrame(weightRows,columns=['draw','Method','wMission','wDistance','wExposure','coefficient','rank']);W.to_csv(out/f'{s}_WEIGHT_SENSITIVITY.csv',index=False)
  B=pd.DataFrame(bootRows,columns=['bootstrap','Method','coefficient','rank','fractionalTop1','nonDominated']);B.to_csv(out/f'{s}_RANKING_BOOTSTRAP.csv',index=False)
  pd.DataFrame(dominanceCounts/bootN,index=range(9),columns=range(9)).to_csv(out/f'{s}_PARETO_PAIRWISE_PROBABILITY.csv')
  if len(B):B.groupby('Method').agg(bootstrapN=('bootstrap','size'),CCmean=('coefficient','mean'),CC95low=('coefficient',lambda x:x.quantile(.025)),CC95high=('coefficient',lambda x:x.quantile(.975)),rankMean=('rank','mean'),fractionalTop1=('fractionalTop1','mean'),nonDominatedProbability=('nonDominated','mean')).to_csv(out/f'{s}_RANK_UNCERTAINTY.csv')
  statuses.append(dict(Scenario=s,status='ESTIMATED' if equal is not None else 'NO_DISCRIMINATING_TOPSIS_CRITERION',bootstrapRequested=bootN,bootstrapRankable=bootN-skipped,weightDrawsExecuted=samples,resampling='scenario/seed blocks' if paired else 'independent within method/scenario'))
 # Explicit scenario-task sensitivity. No renormalization over omitted scenarios.
 task=[];scenarioMeans=data.groupby(['Scenario','Method'])[CRITERIA].mean()
 if not data.TechnicalMissing.any():
  for primary in [0,1,2,3,4]:
   weights=np.ones(4)/4 if primary==0 else np.array([.7 if s==primary else .1 for s in range(1,5)]);M=sum(scenarioMeans.loc[s].reindex(range(9)).to_numpy()*weights[s-1] for s in range(1,5))
   try:r=topsis(M,BENEFIT,PROFILES['equal']);ranks=stats.rankdata(-r['coefficient'],method='min')
   except ValueError:continue
   for m,cc in enumerate(r['coefficient']):task.append(dict(primaryScenario=primary,Method=m,coefficient=cc,rank=ranks[m],scenarioWeights=';'.join(map(str,weights))))
 pd.DataFrame(task,columns=['primaryScenario','Method','coefficient','rank','scenarioWeights']).to_csv(out/'SCENARIO_TASK_SENSITIVITY.csv',index=False);write(out/'RANKING_STATUS.json',statuses)
