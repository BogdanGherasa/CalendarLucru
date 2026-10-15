import * as api from './api.js';
import * as timeline from './timeline.js';
import * as modal from './modal.js';

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

const dateFormatter = new Intl.DateTimeFormat('ro-RO', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric'
});

const container = document.getElementById('timeline');
const wrapper = document.getElementById('timelineWrapper');
const statusEl = document.getElementById('timelineStatus');
const dayLabel = document.getElementById('dayLabel');
const dayError = document.getElementById('dayError');

let currentDate = initialDate();
let loadToken = 0;

function isValidDate(value) {
    if (!DATE_PATTERN.test(value)) {
        return false;
    }
    const [y, m, d] = value.split('-').map(Number);
    const date = new Date(y, m - 1, d);
    return date.getFullYear() === y && date.getMonth() === m - 1 && date.getDate() === d;
}

function initialDate() {
    const fromQuery = new URLSearchParams(window.location.search).get('date');
    return fromQuery && isValidDate(fromQuery) ? fromQuery : timeline.localDateString();
}

function parseLocalDate(value) {
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d);
}

function shiftDate(value, days) {
    const date = parseLocalDate(value);
    date.setDate(date.getDate() + days);
    return timeline.localDateString(date);
}

function formatLabel(value) {
    const text = dateFormatter.format(parseLocalDate(value));
    return text.charAt(0).toLocaleUpperCase('ro-RO') + text.slice(1);
}

function syncUrl() {
    const url = new URL(window.location.href);
    url.searchParams.set('date', currentDate);
    history.replaceState(null, '', url);
}

function setStatus(message) {
    statusEl.textContent = message;
    statusEl.classList.toggle('d-none', !message);
}

function showDayError(message) {
    dayError.textContent = message;
    dayError.classList.toggle('d-none', !message);
}

async function loadDay({ scroll = false } = {}) {
    const token = ++loadToken;
    const date = currentDate;

    dayLabel.textContent = formatLabel(date);
    syncUrl();
    showDayError('');
    setStatus('Se încarcă…');
    wrapper.setAttribute('aria-busy', 'true');

    timeline.updateNowLine(container, date);
    if (scroll) {
        if (date === timeline.localDateString()) {
            timeline.scrollToNow(wrapper);
        } else {
            wrapper.scrollTop = 8 * 60 * timeline.PX_PER_MINUTE;
        }
    }

    try {
        const blocks = await api.list(date);
        if (token !== loadToken) {
            return;
        }
        timeline.render(container, blocks);
        setStatus(blocks.length === 0 ? 'Niciun interval pentru această zi' : '');
    } catch (err) {
        if (token !== loadToken) {
            return;
        }
        timeline.render(container, []);
        setStatus('');
        showDayError(err instanceof api.ApiError ? err.message : 'Nu s-au putut încărca intervalele.');
    } finally {
        if (token === loadToken) {
            wrapper.removeAttribute('aria-busy');
        }
    }
}

function goTo(date) {
    currentDate = date;
    loadDay({ scroll: true });
}

function openCreateDefault() {
    const now = new Date();
    const isToday = currentDate === timeline.localDateString();
    const startMin = isToday
        ? Math.min(Math.ceil((now.getHours() * 60 + now.getMinutes()) / 30) * 30, 23 * 60)
        : 9 * 60;
    modal.openCreate({
        date: currentDate,
        startTime: timeline.fromMinutes(startMin),
        endTime: timeline.fromMinutes(Math.min(startMin + 60, timeline.MINUTES_PER_DAY - 5))
    });
}

document.getElementById('prevDay').addEventListener('click', () => goTo(shiftDate(currentDate, -1)));
document.getElementById('nextDay').addEventListener('click', () => goTo(shiftDate(currentDate, 1)));
document.getElementById('today').addEventListener('click', () => goTo(timeline.localDateString()));
document.getElementById('addBlock').addEventListener('click', openCreateDefault);
document.getElementById('addBlockFab').addEventListener('click', openCreateDefault);

container.addEventListener('block:select', async event => {
    try {
        const block = await api.get(event.detail.id);
        modal.openEdit(block);
    } catch (err) {
        if (err instanceof api.ApiError && err.status === 404) {
            loadDay();
            return;
        }
        showDayError(err instanceof api.ApiError ? err.message : 'Intervalul nu a putut fi încărcat.');
    }
});

container.addEventListener('slot:select', event => {
    modal.openCreate({
        date: currentDate,
        startTime: event.detail.startTime,
        endTime: event.detail.endTime
    });
});

document.addEventListener('blocks:changed', event => {
    const date = event.detail?.date;
    if (date && isValidDate(date) && date !== currentDate) {
        currentDate = date;
        loadDay({ scroll: true });
        return;
    }
    loadDay();
});

modal.init();
timeline.bindInteractions(container);
loadDay({ scroll: true });
