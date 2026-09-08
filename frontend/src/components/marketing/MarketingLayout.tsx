import { Outlet, useLocation } from 'react-router-dom';
import { Suspense, useEffect } from 'react';
import { Spinner } from '../Ui';
import { PublicNav } from './PublicNav';
import { Footer } from './Footer';

/** Chrome shared by every public page. No workspace navigation appears here. */
export function MarketingLayout() {
  const { pathname, hash } = useLocation();

  // A client-side route change keeps the old scroll position, which feels broken on a
  // marketing site. Honour an in-page anchor when there is one, otherwise go to the top.
  useEffect(() => {
    if (hash) {
      const target = document.getElementById(hash.slice(1));
      if (target) {
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        return;
      }
    }
    window.scrollTo(0, 0);
  }, [pathname, hash]);

  return (
    <div className="flex min-h-screen flex-col bg-bg">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-accent focus:px-4 focus:py-2 focus:text-accent-fg"
      >
        Skip to content
      </a>
      <PublicNav />
      {/* pt offsets the fixed floating navbar (h-14 + top offset). */}
      <main id="main" className="flex-1 pt-[4.75rem] sm:pt-[5.25rem]">
        <Suspense fallback={<div className="grid min-h-[60vh] place-items-center"><Spinner /></div>}>
          <Outlet />
        </Suspense>
      </main>
      <Footer />
    </div>
  );
}
