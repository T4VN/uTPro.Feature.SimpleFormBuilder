// Pure date helpers (no DOM / no host). Shared by the entries filter.

/** Format a Date as a local `yyyy-mm-dd` string. */
export function toIsoDate(d) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

/**
 * Resolve a quick-range key to `{ from, to }` as `yyyy-mm-dd` strings.
 * Supported keys: '7d', '30d', 'month'; anything else ('today') => today→today.
 */
export function quickRange(range) {
    const today = new Date();
    let from = new Date(today);
    if (range === '7d') from.setDate(today.getDate() - 6);
    else if (range === '30d') from.setDate(today.getDate() - 29);
    else if (range === 'month') from = new Date(today.getFullYear(), today.getMonth(), 1);
    // 'today' (and any other) keeps from = today
    return { from: toIsoDate(from), to: toIsoDate(today) };
}
