export const PX_PER_MINUTE = 1;
export const MINUTES_PER_DAY = 24 * 60;

const MIN_BLOCK_HEIGHT = 20;
const NOTES_PREVIEW_MIN_HEIGHT = 45;
const SLOT_ROUNDING_MINUTES = 30;
const DEFAULT_DURATION_MINUTES = 60;
// 23:59 rotunjit în jos la multiplu de 5 minute (regula de validare a serverului).
const LATEST_END_MINUTE = MINUTES_PER_DAY - 5;
const NOW_LINE_INTERVAL_MS = 60_000;
const URL_PATTERN = /\bhttps?:\/\/[^\s<>"']+/gi;
const TRAILING_PUNCTUATION = /[.,;:!?)\]}'"»”]+$/;

let nowLineTimer = null;

export function toMinutes(hhmm) {
    const [h, m] = hhmm.split(':').map(Number);
    return h * 60 + m;
}

export function fromMinutes(total) {
    const clamped = Math.max(0, Math.min(total, MINUTES_PER_DAY - 1));
    const h = Math.floor(clamped / 60);
    const m = clamped % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

export function localDateString(date = new Date()) {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
}

export function linkify(text) {
    const fragment = document.createDocumentFragment();
    if (!text) {
        return fragment;
    }

    let lastIndex = 0;
    URL_PATTERN.lastIndex = 0;

    for (const match of text.matchAll(URL_PATTERN)) {
        let url = match[0];
        const trailing = url.match(TRAILING_PUNCTUATION);
        if (trailing) {
            url = url.slice(0, -trailing[0].length);
        }

        const start = match.index;
        if (start > lastIndex) {
            fragment.appendChild(document.createTextNode(text.slice(lastIndex, start)));
        }

        let parsed = null;
        try {
            parsed = new URL(url);
        } catch {
            parsed = null;
        }

        if (parsed && (parsed.protocol === 'http:' || parsed.protocol === 'https:')) {
            const a = document.createElement('a');
            a.href = parsed.href;
            a.target = '_blank';
            a.rel = 'noopener noreferrer';
            a.textContent = url;
            fragment.appendChild(a);
        } else {
            fragment.appendChild(document.createTextNode(url));
        }

        lastIndex = start + url.length;
    }

    if (lastIndex < text.length) {
        fragment.appendChild(document.createTextNode(text.slice(lastIndex)));
    }

    return fragment;
}

function firstLine(text) {
    if (!text) {
        return '';
    }
    const line = text.split(/\r?\n/).find(l => l.trim().length > 0) ?? '';
    return line.trim();
}

function createBlockElement(block) {
    const startMin = toMinutes(block.startTime);
    const endMin = toMinutes(block.endTime);
    const duration = Math.max(endMin - startMin, 0);
    const height = Math.max(duration, MIN_BLOCK_HEIGHT) * PX_PER_MINUTE;

    const el = document.createElement('div');
    el.className = 'timeline-block';
    el.setAttribute('role', 'listitem');
    el.tabIndex = 0;
    el.dataset.id = String(block.id);
    el.style.top = `${startMin * PX_PER_MINUTE}px`;
    el.style.height = `${height}px`;
    if (height < NOTES_PREVIEW_MIN_HEIGHT) {
        el.classList.add('timeline-block--compact');
    }

    const range = `${block.startTime}–${block.endTime}`;
    el.setAttribute('aria-label', `${block.title}, ${range}`);

    const title = document.createElement('span');
    title.className = 'timeline-block-title';
    title.textContent = block.title;

    const time = document.createElement('span');
    time.className = 'timeline-block-time';
    time.textContent = range;

    el.append(title, time);

    const notes = firstLine(block.notes);
    if (height >= NOTES_PREVIEW_MIN_HEIGHT && notes) {
        const notesEl = document.createElement('span');
        notesEl.className = 'timeline-block-notes';
        notesEl.textContent = notes;
        el.appendChild(notesEl);
    }

    return el;
}

export function render(container, blocks) {
    container.querySelectorAll('.timeline-block').forEach(el => el.remove());

    const fragment = document.createDocumentFragment();
    for (const block of blocks) {
        fragment.appendChild(createBlockElement(block));
    }
    container.appendChild(fragment);
}

export function bindInteractions(container) {
    container.addEventListener('click', event => {
        const blockEl = event.target.closest('.timeline-block');
        if (blockEl && container.contains(blockEl)) {
            container.dispatchEvent(new CustomEvent('block:select', {
                bubbles: true,
                detail: { id: Number(blockEl.dataset.id) }
            }));
            return;
        }

        const rect = container.getBoundingClientRect();
        const offsetY = event.clientY - rect.top;
        const minute = Math.floor(offsetY / PX_PER_MINUTE);
        const start = Math.max(0, Math.min(
            Math.floor(minute / SLOT_ROUNDING_MINUTES) * SLOT_ROUNDING_MINUTES,
            MINUTES_PER_DAY - SLOT_ROUNDING_MINUTES));
        const end = Math.min(start + DEFAULT_DURATION_MINUTES, LATEST_END_MINUTE);

        container.dispatchEvent(new CustomEvent('slot:select', {
            bubbles: true,
            detail: { startTime: fromMinutes(start), endTime: fromMinutes(end) }
        }));
    });

    container.addEventListener('keydown', event => {
        if (event.key !== 'Enter' && event.key !== ' ') {
            return;
        }
        const blockEl = event.target.closest('.timeline-block');
        if (blockEl) {
            event.preventDefault();
            blockEl.click();
        }
    });
}

function placeNowLine(container) {
    let line = container.querySelector('.timeline-now');
    if (!line) {
        line = document.createElement('div');
        line.className = 'timeline-now';
        line.setAttribute('aria-hidden', 'true');
        container.appendChild(line);
    }
    const now = new Date();
    const minutes = now.getHours() * 60 + now.getMinutes();
    line.style.top = `${minutes * PX_PER_MINUTE}px`;
}

export function updateNowLine(container, displayedDate) {
    if (nowLineTimer !== null) {
        clearInterval(nowLineTimer);
        nowLineTimer = null;
    }

    const existing = container.querySelector('.timeline-now');

    if (displayedDate !== localDateString()) {
        existing?.remove();
        return;
    }

    placeNowLine(container);
    nowLineTimer = setInterval(() => {
        if (displayedDate !== localDateString()) {
            container.querySelector('.timeline-now')?.remove();
            clearInterval(nowLineTimer);
            nowLineTimer = null;
            return;
        }
        placeNowLine(container);
    }, NOW_LINE_INTERVAL_MS);
}

export function scrollToNow(scrollContainer) {
    const now = new Date();
    const minutes = Math.max(0, now.getHours() * 60 + now.getMinutes() - 60);
    scrollContainer.scrollTop = minutes * PX_PER_MINUTE;
}
