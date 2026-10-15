const BASE_URL = '/api/timeblocks';
const LOGIN_URL = '/Account/Login?returnUrl=/';

export class ApiError extends Error {
    constructor(status, message, fieldErrors = {}) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.fieldErrors = fieldErrors;
    }
}

function csrfToken() {
    const meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.getAttribute('content') ?? '' : '';
}

function defaultMessage(status) {
    switch (status) {
        case 400: return 'Datele trimise nu sunt valide.';
        case 403: return 'Nu ai acces la această resursă.';
        case 404: return 'Intervalul nu a fost găsit.';
        case 409: return 'Intervalul se suprapune cu altul existent.';
        default: return 'A apărut o eroare. Încearcă din nou.';
    }
}

async function parseError(response) {
    let message = defaultMessage(response.status);
    let fieldErrors = {};

    const contentType = response.headers.get('Content-Type') ?? '';
    if (contentType.includes('json')) {
        try {
            const problem = await response.json();
            if (problem && typeof problem === 'object') {
                if (problem.errors && typeof problem.errors === 'object') {
                    fieldErrors = problem.errors;
                }
                if (typeof problem.detail === 'string' && problem.detail.length > 0) {
                    message = problem.detail;
                } else if (typeof problem.title === 'string' && problem.title.length > 0 && response.status !== 400) {
                    message = problem.title;
                }
            }
        } catch {
            // corp invalid: se păstrează mesajul implicit
        }
    }

    return new ApiError(response.status, message, fieldErrors);
}

async function request(method, url, body) {
    const headers = {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': csrfToken()
    };

    let response;
    try {
        response = await fetch(url, {
            method,
            headers,
            credentials: 'same-origin',
            body: body === undefined ? undefined : JSON.stringify(body)
        });
    } catch {
        throw new ApiError(0, 'Nu s-a putut contacta serverul. Verifică conexiunea.');
    }

    if (response.status === 401) {
        window.location.href = LOGIN_URL;
        throw new ApiError(401, 'Sesiunea a expirat.');
    }

    if (!response.ok) {
        throw await parseError(response);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

export function list(date) {
    return request('GET', `${BASE_URL}?date=${encodeURIComponent(date)}`);
}

export function get(id) {
    return request('GET', `${BASE_URL}/${encodeURIComponent(id)}`);
}

export function create(dto) {
    return request('POST', BASE_URL, dto);
}

export function update(id, dto) {
    return request('PUT', `${BASE_URL}/${encodeURIComponent(id)}`, dto);
}

export function remove(id) {
    return request('DELETE', `${BASE_URL}/${encodeURIComponent(id)}`);
}
