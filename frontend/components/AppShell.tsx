"use client";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { authStore } from "@/lib/api";
import { Icon } from "./Icons";

const nav = [
  ["/dashboard", "grid", "Overview"],
  ["/search", "search", "News search"],
  ["/monitors", "radar", "Monitors"],
] as const;

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [email, setEmail] = useState("Analyst");
  useEffect(() => {
    if (!authStore.access()) { router.replace("/login"); return; }
    setEmail(authStore.user()?.email || "Analyst");
  }, [router]);
  const logout = () => { authStore.clear(); router.push("/login"); };

  return <div className="shell">
    <aside className="sidebar">
      <div className="brand"><span className="brandMark">N</span><div><b>Northstar</b><small>News Intelligence</small></div></div>
      <nav>
        <span className="navLabel">Workspace</span>
        {nav.map(([href, icon, label]) => <Link key={href} href={href} className={pathname === href ? "navItem active" : "navItem"}><Icon name={icon}/><span>{label}</span></Link>)}
      </nav>
      <div className="sidebarFoot">
        <div className="userCard"><div className="avatar">{email[0]?.toUpperCase()}</div><div className="userText"><b>{email.split("@")[0]}</b><span>{email}</span></div></div>
        <button className="iconButton" onClick={logout} aria-label="Sign out"><Icon name="logout"/></button>
      </div>
    </aside>
    <main className="main">
      <header className="topbar"><div><span className="eyebrow">INTELLIGENCE WORKSPACE</span></div><div className="topActions"><span className="livePill"><i/> Live signals</span><button className="iconButton"><Icon name="bell"/></button></div></header>
      <div className="content">{children}</div>
    </main>
  </div>;
}
