"use client";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { api, authStore } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email,setEmail]=useState("demo@newsintel.dev"), [password,setPassword]=useState("Demo123!");
  const [loading,setLoading]=useState(false), [error,setError]=useState("");
  async function submit(e:FormEvent){ e.preventDefault(); setLoading(true); setError(""); try { const pair=await api.login(email,password); authStore.save(pair); router.push("/dashboard"); } catch(err){ setError(err instanceof Error?err.message:"Login failed"); } finally { setLoading(false); } }
  return <main className="loginPage"><section className="loginVisual"><div className="visualOrb one"/><div className="visualOrb two"/><div className="loginBrand"><span className="brandMark large">N</span><span>Northstar</span></div><div className="visualCopy"><span className="eyebrow light">NEWS INTELLIGENCE PLATFORM</span><h1>Turn global coverage into <em>clear signals.</em></h1><p>Search, monitor and analyze the news landscape from one focused command center.</p><div className="signalGrid"><div><b>24/7</b><span>Monitoring</span></div><div><b>30d</b><span>Search window</span></div><div><b>AI-ready</b><span>Structured data</span></div></div></div></section><section className="loginPanel"><form className="loginCard" onSubmit={submit}><div><span className="eyebrow">WELCOME BACK</span><h2>Sign in to your workspace</h2><p>Use the seeded demo account or your registered credentials.</p></div><label>Email<input type="email" value={email} onChange={e=>setEmail(e.target.value)} autoComplete="email" required/></label><label>Password<input type="password" value={password} onChange={e=>setPassword(e.target.value)} autoComplete="current-password" required/></label>{error&&<div className="errorBox">{error}</div>}<button className="primaryButton wide" disabled={loading}>{loading?"Signing in…":"Sign in"}</button><div className="demoHint"><b>Demo</b><span>demo@newsintel.dev</span><span>Demo123!</span></div></form></section></main>;
}
