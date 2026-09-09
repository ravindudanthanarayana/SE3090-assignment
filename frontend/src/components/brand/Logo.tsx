import { Link } from 'react-router-dom';
import { routes } from '../../routes';

/**
 * The SmartDesk AI lockup.
 *
 * The mark is the supplied logo, exported with a transparent background so it sits correctly on
 * both themes. The wordmark is set as text rather than as the bitmap: the supplied wordmark is
 * dark navy and would disappear on the dark theme, whereas text picks up the theme's foreground
 * colour, stays crisp at any size and remains readable to assistive technology.
 */
export function LogoMark({ className = 'h-8 w-8' }: { className?: string }) {
  return (
    <img
      src="/logo-mark.png"
      alt=""
      aria-hidden="true"
      width={128}
      height={193}
      className={`${className} object-contain`}
    />
  );
}

interface LogoProps {
  /** Wraps the lockup in a link. Omit inside an element that is already a link. */
  to?: string | null;
  className?: string;
  markClassName?: string;
  textClassName?: string;
}

export function Logo({
  to = routes.home,
  className = '',
  markClassName = 'h-7 w-7',
  textClassName = 'text-[17px]',
}: LogoProps) {
  const content = (
    <span className={`inline-flex items-center gap-2.5 ${className}`}>
      <LogoMark className={markClassName} />
      <span className={`whitespace-nowrap font-semibold tracking-tight text-fg ${textClassName}`}>
        SmartDesk<span className="text-accent"> AI</span>
      </span>
    </span>
  );

  if (!to) return content;

  return (
    <Link to={to} className="rounded-full" aria-label="SmartDesk AI, home">
      {content}
    </Link>
  );
}
