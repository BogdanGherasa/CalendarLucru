import * as api from './api.js';
import { linkify, toMinutes } from './timeline.js';

const TITLE_MAX = 120;
const NOTES_MAX = 5000;
const MINUTE_STEP = 5;

const FIELD_KEYS = ['title', 'date', 'startTime', 'endTime', 'notes'];

const MODE_TITLES = {
    create: 'Interval nou',
    view: 'Detalii interval',
    edit: 'Editează intervalul'
};

let modalEl;
let bsModal;
let form;
let fields;
let alertEl;
let saveBtn;
let deleteBtn;
let editBtn;
let cancelBtn;
let titleEl;
let counterEl;
let previewWrapper;
let previewEl;
let viewTime;
let viewTitle;
let viewNotes;
let currentBlock = null;
let busy = false;

export function init() {
    modalEl = document.getElementById('blockModal');
    bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
    form = document.getElementById('blockForm');
    alertEl = document.getElementById('blockFormAlert');
    saveBtn = document.getElementById('saveBlock');
    deleteBtn = document.getElementById('deleteBlock');
    editBtn = document.getElementById('editBlock');
    cancelBtn = document.getElementById('cancelBlock');
    titleEl = document.getElementById('blockModalTitle');
    counterEl = document.getElementById('notesCounter');
    previewWrapper = document.getElementById('notesPreviewWrapper');
    previewEl = document.getElementById('notesPreview');
    viewTime = document.getElementById('viewTime');
    viewTitle = document.getElementById('viewTitle');
    viewNotes = document.getElementById('viewNotes');

    fields = {
        title: document.getElementById('fieldTitle'),
        date: document.getElementById('fieldDate'),
        startTime: document.getElementById('fieldStart'),
        endTime: document.getElementById('fieldEnd'),
        notes: document.getElementById('fieldNotes')
    };

    form.addEventListener('submit', onSubmit);
    deleteBtn.addEventListener('click', onDelete);
    editBtn.addEventListener('click', () => {
        if (currentBlock) {
            setMode('edit');
            fields.title.focus();
        }
    });
    fields.notes.addEventListener('input', () => {
        updateCounter();
        updatePreview();
    });
    for (const key of FIELD_KEYS) {
        fields[key].addEventListener('input', () => clearFieldError(key));
    }

    modalEl.addEventListener('hidden.bs.modal', () => {
        currentBlock = null;
        resetForm();
    });
}

export function openCreate(prefill = {}) {
    currentBlock = null;
    resetForm();
    fields.title.value = '';
    fields.date.value = prefill.date ?? '';
    fields.startTime.value = prefill.startTime ?? '';
    fields.endTime.value = prefill.endTime ?? '';
    fields.notes.value = '';
    updateCounter();
    updatePreview();
    setMode('create');
    bsModal.show();
    modalEl.addEventListener('shown.bs.modal', () => fields.title.focus(), { once: true });
}

export function openEdit(block) {
    currentBlock = block;
    resetForm();
    fields.title.value = block.title;
    fields.date.value = block.date;
    fields.startTime.value = block.startTime;
    fields.endTime.value = block.endTime;
    fields.notes.value = block.notes ?? '';
    updateCounter();
    updatePreview();
    renderView(block);
    setMode('view');
    bsModal.show();
}

function setMode(mode) {
    modalEl.dataset.mode = mode;
    titleEl.textContent = MODE_TITLES[mode];

    const isView = mode === 'view';
    document.getElementById('blockView').classList.toggle('d-none', !isView);
    form.classList.toggle('d-none', isView);

    saveBtn.classList.toggle('d-none', isView);
    editBtn.classList.toggle('d-none', !isView);
    deleteBtn.classList.toggle('d-none', mode !== 'edit');
    cancelBtn.textContent = isView ? 'Închide' : 'Anulează';
}

function renderView(block) {
    viewTime.textContent = `${block.date} · ${block.startTime}–${block.endTime}`;
    viewTitle.textContent = block.title;
    viewNotes.replaceChildren();
    if (block.notes) {
        viewNotes.appendChild(linkify(block.notes));
    } else {
        const empty = document.createElement('p');
        empty.className = 'text-body-secondary fst-italic mb-0';
        empty.textContent = 'Fără notițe.';
        viewNotes.appendChild(empty);
    }
}

function updateCounter() {
    const length = fields.notes.value.length;
    counterEl.textContent = `${length} / ${NOTES_MAX}`;
    counterEl.classList.toggle('text-danger', length > NOTES_MAX);
}

function updatePreview() {
    const text = fields.notes.value;
    const hasLink = /https?:\/\//i.test(text);
    previewWrapper.classList.toggle('d-none', !hasLink);
    previewEl.replaceChildren();
    if (hasLink) {
        previewEl.appendChild(linkify(text));
    }
}

function resetForm() {
    form.reset();
    hideAlert();
    for (const key of FIELD_KEYS) {
        clearFieldError(key);
    }
    setBusy(false);
}

function readDto() {
    const notes = fields.notes.value.trim();
    return {
        title: fields.title.value.trim(),
        date: fields.date.value,
        startTime: fields.startTime.value.slice(0, 5),
        endTime: fields.endTime.value.slice(0, 5),
        notes: notes.length > 0 ? notes : null
    };
}

function validate(dto) {
    const errors = {};

    if (!dto.title) {
        errors.title = ['Titlul este obligatoriu.'];
    } else if (dto.title.length > TITLE_MAX) {
        errors.title = [`Titlul poate avea maxim ${TITLE_MAX} de caractere.`];
    }

    if (!/^\d{4}-\d{2}-\d{2}$/.test(dto.date)) {
        errors.date = ['Data este obligatorie.'];
    }

    const timePattern = /^\d{2}:\d{2}$/;
    const startOk = timePattern.test(dto.startTime);
    const endOk = timePattern.test(dto.endTime);
    if (!startOk) {
        errors.startTime = ['Ora de început este obligatorie.'];
    } else if (toMinutes(dto.startTime) % MINUTE_STEP !== 0) {
        errors.startTime = [`Minutele trebuie să fie multiplu de ${MINUTE_STEP}.`];
    }
    if (!endOk) {
        errors.endTime = ['Ora de sfârșit este obligatorie.'];
    } else if (toMinutes(dto.endTime) % MINUTE_STEP !== 0) {
        errors.endTime = [`Minutele trebuie să fie multiplu de ${MINUTE_STEP}.`];
    }
    if (!errors.startTime && !errors.endTime && toMinutes(dto.endTime) <= toMinutes(dto.startTime)) {
        errors.endTime = ['Ora de sfârșit trebuie să fie după ora de început.'];
    }

    if (dto.notes && dto.notes.length > NOTES_MAX) {
        errors.notes = [`Notițele pot avea maxim ${NOTES_MAX} de caractere.`];
    }

    return errors;
}

function showFieldErrors(errors) {
    let firstInvalid = null;
    for (const [rawKey, messages] of Object.entries(errors)) {
        const key = rawKey.charAt(0).toLowerCase() + rawKey.slice(1);
        const input = fields[key];
        const feedback = form.querySelector(`[data-error-for="${key}"]`);
        if (!input || !feedback) {
            continue;
        }
        input.classList.add('is-invalid');
        feedback.textContent = Array.isArray(messages) ? messages.join(' ') : String(messages);
        firstInvalid ??= input;
    }
    firstInvalid?.focus();
    return firstInvalid !== null;
}

function clearFieldError(key) {
    fields[key].classList.remove('is-invalid');
    const feedback = form.querySelector(`[data-error-for="${key}"]`);
    if (feedback) {
        feedback.textContent = '';
    }
}

function showAlert(message) {
    alertEl.textContent = message;
    alertEl.classList.remove('d-none');
}

function hideAlert() {
    alertEl.textContent = '';
    alertEl.classList.add('d-none');
}

function setBusy(value) {
    busy = value;
    saveBtn.disabled = value;
    deleteBtn.disabled = value;
    editBtn.disabled = value;
}

function emitChanged(date) {
    document.dispatchEvent(new CustomEvent('blocks:changed', { detail: { date } }));
}

async function onSubmit(event) {
    event.preventDefault();
    if (busy) {
        return;
    }

    hideAlert();
    for (const key of FIELD_KEYS) {
        clearFieldError(key);
    }

    const dto = readDto();
    const errors = validate(dto);
    if (Object.keys(errors).length > 0) {
        showFieldErrors(errors);
        return;
    }

    setBusy(true);
    try {
        const saved = currentBlock
            ? await api.update(currentBlock.id, dto)
            : await api.create(dto);
        bsModal.hide();
        emitChanged(saved.date);
    } catch (err) {
        handleError(err);
    } finally {
        setBusy(false);
    }
}

async function onDelete() {
    if (busy || !currentBlock) {
        return;
    }
    if (!window.confirm('Ștergi acest interval?')) {
        return;
    }

    const date = currentBlock.date;
    setBusy(true);
    try {
        await api.remove(currentBlock.id);
        bsModal.hide();
        emitChanged(date);
    } catch (err) {
        if (err instanceof api.ApiError && err.status === 404) {
            bsModal.hide();
            emitChanged(date);
            return;
        }
        handleError(err);
    } finally {
        setBusy(false);
    }
}

function handleError(err) {
    if (!(err instanceof api.ApiError)) {
        showAlert('A apărut o eroare neașteptată.');
        return;
    }

    if (err.status === 400 && err.fieldErrors && Object.keys(err.fieldErrors).length > 0) {
        if (modalEl.dataset.mode === 'view') {
            setMode('edit');
        }
        if (!showFieldErrors(err.fieldErrors)) {
            showAlert(err.message);
        }
        return;
    }

    showAlert(err.message);
}
