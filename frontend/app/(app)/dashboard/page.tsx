"use client";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { Dashboard } from "@/lib/types";
import { AreaChart, SentimentDonut, SourcesBars } from "@/components/Charts";
import { ArticleList } from "@/components/ArticleList";

export default function DashboardPage(){
 const [data,setData]=useState<Dashboard|null>(null),[error,setError]=useState("");
 useEffect(()=>{api.dashboard().then(setData).catch(e=>setError(e.message))},[]);
 if(error) return <div className="errorBox">{error}</div>;
 if(!data) return <Loading/>;
 return <><div className="pageHead"><div><span className="eyebrow">OVERVIEW</span><h1>Intelligence dashboard</h1><p>Your monitored news landscape, condensed into the signals that matter.</p></div><span className="providerPill">● {data.provider}</span></div>
 <section className="metricGrid">{data.metrics.map(m=><div className="metricCard" key={m.label} data-tone={m.tone}><div className="metricTop"><span>{m.label}</span><i/></div><strong>{m.value}</strong><small>{m.delta}</small></div>)}</section>
 <section className="dashboardGrid"><div className="panel span2"><div className="panelHead"><div><span className="eyebrow">TREND</span><h2>News volume</h2></div><span className="periodChip">Last 14 days</span></div><AreaChart data={data.volume}/></div><div className="panel"><div className="panelHead"><div><span className="eyebrow">SOURCES</span><h2>Share of voice</h2></div></div><SourcesBars data={data.sources}/></div><div className="panel"><div className="panelHead"><div><span className="eyebrow">SENTIMENT</span><h2>Coverage tone</h2></div></div><SentimentDonut {...data.sentiment}/></div><div className="panel span2"><div className="panelHead"><div><span className="eyebrow">LATEST SIGNALS</span><h2>Recent monitored coverage</h2></div></div><ArticleList articles={data.recentArticles}/></div></section></>;
}
function Loading(){return <><div className="pageHead skeletonHead"/><section className="metricGrid">{[1,2,3,4].map(x=><div className="metricCard skeleton" key={x}/>)}</section><div className="panel skeleton tall"/></>}
