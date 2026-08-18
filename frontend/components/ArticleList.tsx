import type { Article } from "@/lib/types";
import { Icon } from "./Icons";

function timeAgo(value: string) {
  const minutes = Math.max(1, Math.round((Date.now()-new Date(value).getTime())/60000));
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes/60); if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours/24)}d ago`;
}

export function ArticleList({ articles }: { articles: Article[] }) {
  if (!articles.length) return <div className="emptyState"><span>◎</span><h3>No articles yet</h3><p>Run a monitor or search for a topic to start collecting intelligence.</p></div>;
  return <div className="articleList">{articles.map(article => <article className="article" key={article.id}><div className="articleAccent" data-tone={article.sentiment}/><div className="articleBody"><div className="articleMeta"><span>{article.source}</span><i/> <span>{timeAgo(article.publishedAt)}</span><span className={`sentiment ${article.sentiment}`}>{article.sentiment}</span></div><h3>{article.title}</h3><p>{article.description}</p><div className="articleFoot"><span>{article.language || "—"}</span><a href={article.url} target="_blank" rel="noreferrer">Open source <Icon name="external" width="14" height="14"/></a></div></div></article>)}</div>;
}
