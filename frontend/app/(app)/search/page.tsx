"use client";
import { FormEvent, useState } from "react";
import { api } from "@/lib/api";
import type { SearchResult } from "@/lib/types";
import { ArticleList } from "@/components/ArticleList";
import { Icon } from "@/components/Icons";

export default function SearchPage(){
 const [query,setQuery]=useState("OpenAI enterprise AI"),[days,setDays]=useState(7),[language,setLanguage]=useState("english");
 const [result,setResult]=useState<SearchResult|null>(null),[loading,setLoading]=useState(false),[error,setError]=useState("");
 async function search(e?:FormEvent){e?.preventDefault();setLoading(true);setError("");try{setResult(await api.search(query,days,language,24))}catch(err){setError(err instanceof Error?err.message:"Search failed")}finally{setLoading(false)}}
 return <><div className="pageHead"><div><span className="eyebrow">DISCOVERY</span><h1>News search</h1><p>Explore recent coverage with a focused query and server-side Webz.io integration.</p></div></div><form className="searchPanel" onSubmit={search}><div className="searchInput"><Icon name="search"/><input value={query} onChange={e=>setQuery(e.target.value)} placeholder="Company, market, topic or Boolean query…"/><button className="primaryButton" disabled={loading}>{loading?"Searching…":"Search news"}</button></div><div className="filterRow"><label>Window<select value={days} onChange={e=>setDays(Number(e.target.value))}><option value={1}>24 hours</option><option value={3}>3 days</option><option value={7}>7 days</option><option value={14}>14 days</option><option value={30}>30 days</option></select></label><label>Language<select value={language} onChange={e=>setLanguage(e.target.value)}><option value="english">English</option><option value="german">German</option><option value="french">French</option><option value="spanish">Spanish</option><option value="all">All</option></select></label><div className="filterNote"><span>Tip</span> Webz query syntax can be entered directly in the search field.</div></div></form>{error&&<div className="errorBox">{error}</div>}{result&&<section className="results"><div className="resultsHead"><div><span className="eyebrow">RESULTS</span><h2>{result.totalResults.toLocaleString()} matching articles</h2></div><span className="providerPill">● {result.provider}</span></div><ArticleList articles={result.items}/></section>}</>;
}
