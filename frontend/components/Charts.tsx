import type { Point, SourceStat } from "@/lib/types";

export function AreaChart({ data }: { data: Point[] }) {
  const width = 760, height = 220, pad = 18;
  const max = Math.max(...data.map(x => x.value), 1);
  const points = data.map((p, i) => {
    const x = pad + i * ((width - pad * 2) / Math.max(1, data.length - 1));
    const y = height - pad - (p.value / max) * (height - pad * 2);
    return [x, y] as const;
  });
  const line = points.map(([x,y], i) => `${i ? "L" : "M"}${x},${y}`).join(" ");
  const area = points.length ? `${line} L${points.at(-1)![0]},${height-pad} L${points[0][0]},${height-pad} Z` : "";
  return <div className="chartWrap"><svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="News volume over time">
    <defs><linearGradient id="areaFill" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="currentColor" stopOpacity=".28"/><stop offset="100%" stopColor="currentColor" stopOpacity="0"/></linearGradient></defs>
    {[.25,.5,.75].map(n => <line key={n} x1={pad} x2={width-pad} y1={height*n} y2={height*n} className="gridLine"/>)}
    <path d={area} fill="url(#areaFill)" className="chartArea"/><path d={line} fill="none" className="chartLine"/>
    {points.map(([x,y], i) => <circle key={i} cx={x} cy={y} r="3" className="chartDot"/>)}
  </svg><div className="chartLabels">{data.filter((_,i)=>i%3===0 || i===data.length-1).map(x=><span key={x.label}>{x.label}</span>)}</div></div>;
}

export function SourcesBars({ data }: { data: SourceStat[] }) {
  return <div className="sourceList">{data.length ? data.map(item => <div className="sourceRow" key={item.name}><div className="sourceMeta"><span>{item.name}</span><b>{item.percentage}%</b></div><div className="barTrack"><span style={{width:`${Math.max(4,item.percentage)}%`}}/></div></div>) : <Empty/>}</div>;
}

export function SentimentDonut({ positive, neutral, negative }: { positive:number; neutral:number; negative:number }) {
  const total = Math.max(1, positive+neutral+negative), pp=Math.round(positive/total*100), np=Math.round(neutral/total*100);
  return <div className="sentimentBlock"><div className="donut" style={{background:`conic-gradient(#7c6cff 0 ${pp}%, #3ed6a2 ${pp}% ${pp+np}%, #ff6d7a ${pp+np}% 100%)`}}><div><strong>{positive+neutral+negative}</strong><span>articles</span></div></div><div className="legend"><span><i className="violetDot"/>Positive <b>{positive}</b></span><span><i className="greenDot"/>Neutral <b>{neutral}</b></span><span><i className="redDot"/>Negative <b>{negative}</b></span></div></div>;
}
function Empty(){ return <div className="emptyMini">Run a monitor to populate analytics.</div> }
