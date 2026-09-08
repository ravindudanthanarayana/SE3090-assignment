import type { ReactNode } from 'react';

/**
 * Consistent vertical rhythm and max width for every marketing section.
 *
 * A section that carries its own background is rendered as an inset rounded slab rather than a
 * full-bleed band, so the boundary between sections is a soft corner instead of a hard edge
 * running the width of the screen. A `default` section shares the page background, so there is
 * no visible cut to round.
 */
export function Section({
  children, className = '', tone = 'default', id,
}: {
  children: ReactNode; className?: string; tone?: 'default' | 'subtle' | 'glow'; id?: string;
}) {
  const inner = (
    <div className="mx-auto max-w-7xl px-4 py-20 sm:px-6 lg:px-8 lg:py-28">{children}</div>
  );

  if (tone === 'default') {
    return <section id={id} className={className}>{inner}</section>;
  }

  const tones = {
    subtle: 'bg-bg-subtle',
    glow: 'bg-bg-subtle sd-glow',
  };

  return (
    <section id={id} className={`px-2 sm:px-4 ${className}`}>
      <div className={`overflow-hidden ${SLAB} ${tones[tone]}`}>{inner}</div>
    </section>
  );
}

/** Shared slab radius, so every rounded section cut matches. */
export const SLAB = 'rounded-[1.75rem] sm:rounded-[2.25rem] lg:rounded-[2.75rem]';

export function Eyebrow({ children }: { children: ReactNode }) {
  return (
    <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent-text">{children}</p>
  );
}

export function SectionHeading({
  eyebrow, title, description, align = 'left', className = '',
}: {
  eyebrow?: string;
  title: ReactNode;
  description?: ReactNode;
  align?: 'left' | 'center';
  className?: string;
}) {
  return (
    <div className={`${align === 'center' ? 'mx-auto max-w-3xl text-center' : 'max-w-3xl'} ${className}`}>
      {eyebrow && <Eyebrow>{eyebrow}</Eyebrow>}
      <h2 className="mt-3 text-3xl font-semibold leading-[1.15] tracking-tight text-fg sm:text-4xl lg:text-[2.75rem]">
        {title}
      </h2>
      {description && (
        <p className="mt-5 text-lg leading-relaxed text-fg-muted">{description}</p>
      )}
    </div>
  );
}
