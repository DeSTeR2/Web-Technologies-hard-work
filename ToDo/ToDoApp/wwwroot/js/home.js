const listsEndpoint = '/api/TodoList';

const listsContainer = document.getElementById('listsContainer');
const listCardTemplate = document.getElementById('listCardTemplate');

async function fetchLists() {
    try {
        const res = await fetch(`${listsEndpoint}/ByActiveUser`);
        if (!res.ok) throw new Error('Could not load lists');
        const data = await res.json();
        renderLists(data.$values || []);
    } catch (e) {
        listsContainer.innerHTML = '<div class="empty">Unable to load lists.</div>';
        console.error(e);
    }
}

function renderLists(items) {
    listsContainer.innerHTML = '';
    if (!items || items.length === 0) {
        listsContainer.innerHTML = '<div class="empty">No lists yet. Create one!</div>';
        return;
    }

    items.forEach(item => {
        const node = listCardTemplate.content.cloneNode(true);
        const card = node.querySelector('.list-card');

        const id = item.id ?? item.Id;
        const name = item.name ?? item.Name;
        const noteIds = item.noteIds.$values ?? item.NoteIds.$values ?? [];

        card.dataset.id = id;
        card.querySelector('.list-title').textContent = name ?? '(no name)';
        card.querySelector('.count').textContent = (Array.isArray(noteIds) ? noteIds.length : 0);

        node.querySelector('.btn-open').addEventListener('click', () => {
            window.location.href = `/ListDetails?id=${encodeURIComponent(id)}`;
        });

        node.querySelector('.btn-delete-list').addEventListener('click', async () => {
            if (!confirm('Delete list "' + (name ?? id) + '"?')) return;
            await deleteList(id);
            await fetchLists();
        });

        listsContainer.appendChild(node);
    });
}

async function createList(name) {
    try {
        const url = `${listsEndpoint}?name=${encodeURIComponent(name)}`;
        const res = await fetch(url, { method: 'POST' });
        if (!res.ok) {
            const txt = await res.text().catch(() => null);
            throw new Error('Create list failed' + (txt ? ': ' + txt : ''));
        }
        await fetchLists();
    } catch (e) {
        alert('Unable to create list');
        console.error(e);
    }
}

async function deleteList(id) {
    try {
        const res = await fetch(`${listsEndpoint}/${encodeURIComponent(id)}`, { method: 'DELETE' });
        if (!res.ok) {
            const txt = await res.text().catch(() => null);
            throw new Error('Delete failed' + (txt ? ': ' + txt : ''));
        }
    } catch (e) {
        console.error(e);
        alert('Unable to delete list');
    }
}

document.getElementById('createListBtn').addEventListener('click', () => {
    const nameInput = document.getElementById('listNameInput');
    const name = nameInput.value.trim();
    if (!name) return;
    createList(name);
    nameInput.value = '';
});

fetchLists();
