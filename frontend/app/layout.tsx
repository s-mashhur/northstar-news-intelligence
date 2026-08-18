import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Northstar — News Intelligence",
  description: "Portfolio-grade news monitoring and intelligence platform powered by ASP.NET Core, Next.js and Webz.io.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
