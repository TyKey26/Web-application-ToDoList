const API = '/api/todos';

const listEl         = document.getElementById('todoList');
const formEl         = document.getElementById('addForm');
const inputEl        = document.getElementById('newTitle');
const searchEl       = document.getElementById('search');
const emptyEl        = document.getElementById('empty');
const statsEl        = document.getElementById('stats');
const clearBtn       = document.getElementById('clearCompleted');

let todos = [];
let filter = 'all';
let search = '';

// ---------- API ----------
async function fetchTodos() {
    const params = new URLSearchParams();
    if (filter !== 'all') params.set('filter', filter);
    if (search) params.set('search', search);

    const res = await fetch(`${API}?${params}`);
    if (!res.ok) throw new Error('Ошибка загрузки');
    return res.json();
}

async function apiAdd(title) {
    const res = await fetch(API, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title })
    });
    if (!res.ok) throw new Error('Ошибка добавления');
    return res.json();
}

async function apiUpdate(id, patch) {
    const res = await fetch(`${API}/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(patch)
    });
    return res.json();
}

async function apiDelete(id) {
    await fetch(`${API}/${id}`, { method: 'DELETE' });
}

async function apiClearCompleted() {
    const res = await fetch(`${API}/completed`, { method: 'DELETE' });
    return res.json();
}

async function apiStats() {
    const res = await fetch(`${API}/stats`);
    return res.json();
}

// ---------- Render ----------
function render() {
    listEl.innerHTML = '';

    if (todos.length === 0) {
        emptyEl.hidden = false;
    } else {
        emptyEl.hidden = true;

        for (const todo of todos) {
            const li = document.createElement('li');
            li.className = 'todo-item' + (todo.completed ? ' completed' : '');

            const cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.checked = todo.completed;
            cb.addEventListener('change', () => toggleTodo(todo.id, cb.checked));

            const title = document.createElement('span');
            title.className = 'title';
            title.textContent = todo.title;

            const del = document.createElement('button');
            del.className = 'delete';
            del.textContent = '✕';
            del.title = 'Удалить';
            del.addEventListener('click', () => removeTodo(todo.id));

            li.append(cb, title, del);
            listEl.appendChild(li);
        }
    }
}

async function updateStats() {
    try {
        const s = await apiStats();
        statsEl.textContent = `Всего: ${s.total} · Активных: ${s.active} · Выполнено: ${s.completed}`;
        clearBtn.hidden = s.completed === 0;
    } catch { /* ignore */ }
}

// ---------- Actions ----------
async function load() {
    try {
        todos = await fetchTodos();
        render();
        await updateStats();
    } catch (err) {
        console.error(err);
        emptyEl.hidden = false;
        emptyEl.textContent = 'Не удалось загрузить задачи';
    }
}

async function addTodo(title) {
    await apiAdd(title);
    await load();
}

async function toggleTodo(id, completed) {
    await apiUpdate(id, { completed });
    await load();
}

async function removeTodo(id) {
    await apiDelete(id);
    await load();
}

// ---------- Events ----------
formEl.addEventListener('submit', async (e) => {
    e.preventDefault();
    const title = inputEl.value.trim();
    if (!title) return;
    inputEl.value = '';
    await addTodo(title);
});

document.querySelectorAll('.filters button').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.filters button').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        filter = btn.dataset.filter;
        load();
    });
});

let searchTimer;
searchEl.addEventListener('input', () => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => {
        search = searchEl.value.trim();
        load();
    }, 250);
});

clearBtn.addEventListener('click', async () => {
    if (!confirm('Удалить все выполненные задачи?')) return;
    await apiClearCompleted();
    await load();
});

// ---------- Init ----------
load();