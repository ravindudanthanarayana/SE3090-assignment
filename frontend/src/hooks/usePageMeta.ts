import { useEffect } from 'react';

/**
 * Sets the document title and meta description per route.
 *
 * This is a single-page app, so the tags in index.html only describe the first load. Keeping
 * them current per page matters for the browser tab, for bookmarks and for anything that reads
 * the rendered document.
 */
export function usePageMeta(title: string, description?: string) {
  useEffect(() => {
    document.title = title;

    if (!description) return;

    let tag = document.querySelector<HTMLMetaElement>('meta[name="description"]');
    if (!tag) {
      tag = document.createElement('meta');
      tag.name = 'description';
      document.head.appendChild(tag);
    }

    const previous = tag.content;
    tag.content = description;
    return () => { tag.content = previous; };
  }, [title, description]);
}
