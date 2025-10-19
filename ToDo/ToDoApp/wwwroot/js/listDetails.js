// visible statuses (exclude None=0)
const VisibleStatuses = [1,2,3,4];
const statusNames = { 1:'Waiting',2:'In Process',3:'Disabled',4:'Completed' };

const listsEndpoint = '/api/TodoList';
const todoApi = '/api/TodoNote';

const board = document.getElementById('board');
const backBtn = document.getElementById('backBtn');
const listTitle = document.getElementById('listTitle');
const listMeta = document.getElementById('listMeta');

// modal elements
const modalBackdrop = document.getElementById('modalBackdrop');
const modalTitleInput = document.getElementById('modalTitleInput');
const modalContextInput = document.getElementById('modalContextInput');
const modalEndInput = document.getElementById('modalEndInput');
const modalStatusSelect = document.getElementById('modalStatusSelect');
const modalCancel = document.getElementById('modalCancel');
const modalCreate = document.getElementById('modalCreate');

let listId = null;

function setListId(id) {
    listId = id;
}

function buildColumns(){
    board.innerHTML = '';
    VisibleStatuses.forEach(status =>{
        const col = document.createElement('div');
        col.className = 'column';
        col.dataset.status = status;
        col.innerHTML = `
            <div class="col-header">
                <div style="display:flex;align-items:center;gap:8px">
                    <div class="col-title">${escapeHtml(statusNames[status])}</div>
                    <div class="col-count" data-count>0</div>
                </div>
                <button class="col-add" data-add-status="${status}">+ Add</button>
            </div>
            <div class="col-body"></div>`;
        board.appendChild(col);
    });

    board.querySelectorAll('.col-add').forEach(btn =>{
        btn.addEventListener('click', ()=> openCreateModal(Number(btn.dataset.addStatus)));
    });
}

async function loadList(){
    try{
        if(!listId) throw new Error('listId not set');
        const res = await fetch(`${listsEndpoint}/${encodeURIComponent(listId)}`);
        if(!res.ok) throw new Error('failed to load list');
        const list = await res.json();

        const name = list.name ?? list.Name ?? 'Unnamed';
        const noteIds = Array.isArray(list.noteIds.$values) ? list.noteIds.$values : (Array.isArray(list.NoteIds.$values) ? list.NoteIds.$values : []);
        listTitle.textContent = name;
        listMeta.textContent = `${noteIds.length} tasks`;

        buildColumns();
        await loadTodos(noteIds);
    }catch(e){
        listTitle.textContent = 'Error loading list';
        console.error(e);
        board.innerHTML = '<div class="empty">Could not load list</div>';
    }
}

async function loadTodos(noteIds){
    try{
        // Expect noteIds as array of strings
        const ids = Array.isArray(noteIds) ? noteIds : [];

        if(ids.length === 0){
            renderBoard([]);
            return;
        }

        // fetch notes in parallel
        const fetches = ids.map(id => fetch(`${todoApi}/${encodeURIComponent(id)}`)
            .then(r => {
                if(!r.ok) return r.text().then(t => { throw new Error(`Failed to load note ${id}: ${r.status} ${t}`); });
                return r.json();
            })
        );

        const rawNotes = await Promise.all(fetches);

        // normalize notes to the UI shape: { id, title, content, endDate, status }
        const notes = rawNotes.map(n => ({
            id: n.id ?? n.Id ?? n._id ?? n._Id,
            title: n.name ?? n.Name ?? n.title ?? n.Title ?? '',
            content: n.content ?? n.Content ?? n.Context ?? '',
            endDate: n.endDate ?? n.EndDate ?? n.EndedAt ?? null,
            status: (typeof n.status !== 'undefined') ? n.status : (typeof n.Status !== 'undefined' ? n.Status : 0)
        }));

        renderBoard(notes);
    }catch(e){
        board.innerHTML = '<div class="empty">Could not load tasks</div>';
        console.error('loadTodos error', e);
    }
}

function renderCard(item) {
    const card = document.createElement('div');
    card.className = 'card';
    card.dataset.id = item.id;

    const maxLength = 60;
    const previewContent = item.content
        ? (item.content.length > maxLength ? item.content.slice(0, maxLength) + '...' : item.content)
        : '';

    // use textContent on editable fields to avoid HTML injection
    card.innerHTML = `
        <div class="left">
            <div class="title" contenteditable="true">${escapeHtml(item.title)}</div>
            <div class="content" contenteditable="true">${escapeHtml(previewContent)}</div>
        </div>
        <div style="display:flex;flex-direction:column;align-items:flex-end;gap:8px">
            <div class="status-display" style="cursor:pointer;padding:2px 6px;border-radius:4px;border:1px solid #e0e0e0;">
                ${escapeHtml(statusNames[item.status] || 'Unknown')}
            </div>
            <div class="actions" style="display:flex;gap:6px;margin-top:6px">
                <button class="btn btn-ghost btn-delete">Delete</button>
            </div>
        </div>
    `;

    // Open modal for full editing
    card.querySelector('.left').addEventListener('click', () => openEditModal(item));
    card.querySelector('.status-display').addEventListener('click', () => openEditModal(item));

    // Inline editing (title)
    const titleEl = card.querySelector('.title');
    titleEl.addEventListener('blur', async (e) => {
        const newTitle = e.target.textContent.trim();
        if(newTitle && newTitle !== item.title){
            try { await updateTodoContent(item.id, newTitle); item.title = newTitle; } 
            catch(err){ console.error(err); e.target.textContent = item.title; }
        } else e.target.textContent = item.title;
    });

    // Inline editing (content)
    const contentEl = card.querySelector('.content');
    contentEl.addEventListener('blur', async (e) => {
        const newContent = e.target.textContent.trim();
        if(newContent !== item.content){
            try { await updateTodoContent(item.id, newContent); item.content = newContent; } 
            catch(err){ console.error(err); e.target.textContent = item.content ? (item.content.length > maxLength ? item.content.slice(0, maxLength) + '...' : item.content) : ''; }
        } else {
            // restore preview formatting
            e.target.textContent = item.content ? (item.content.length > maxLength ? item.content.slice(0, maxLength) + '...' : item.content) : '';
        }
    });

    // Delete
    card.querySelector('.btn-delete').addEventListener('click', async (e) => {
        e.stopPropagation();
        if(!confirm('Delete this task?')) return;
        try{
            await deleteTodo(item.id);
            // after delete, reload the list
            await loadList();
        } catch(err){
            console.error(err);
            alert('Unable to delete task');
        }
    });

    return card;
}

function renderBoard(items){
    buildColumns();
    const buckets = {1:[],2:[],3:[],4:[]};
    if(!Array.isArray(items)) items = [];

    items.forEach(it=>{
        const s = Number(it.status) || 0;
        if(buckets[s]) buckets[s].push(it);
        else buckets[1].push(it); // fallback to Waiting
    });

    Object.keys(buckets).forEach(statusKey =>{
        const col = board.querySelector(`.column[data-status='${statusKey}']`);
        if(!col) return;
        const body = col.querySelector('.col-body');
        body.innerHTML = '';
        buckets[statusKey].forEach(item => {
            const card = renderCard(item);
            body.appendChild(card);
        });

        const countEl = col.querySelector('[data-count]');
        countEl.textContent = String(buckets[statusKey].length);
    });
}

function openEditModal(item) {
    modalTitleInput.value = item.title || '';
    modalContextInput.value = item.content || '';
    modalEndInput.value = item.endDate ? new Date(item.endDate).toISOString().slice(0,16) : '';
    modalStatusSelect.value = String(item.status || VisibleStatuses[0]);
    modalBackdrop.style.display = 'flex';
    modalBackdrop.setAttribute('aria-hidden','false');

    modalCreate.onclick = async () => {
        const newTitle = modalTitleInput.value.trim();
        const newContent = modalContextInput.value.trim();
        const newEnd = modalEndInput.value ? new Date(modalEndInput.value).toISOString() : null;
        const newStatus = Number(modalStatusSelect.value);

        try {
            if(newTitle !== item.title) await updateTodoContent(item.id, newTitle);
            if(newContent !== item.content) await updateTodoContent(item.id, newContent);
            if(newStatus !== item.status) await updateTodoStatus(item.id, newStatus);
            if(newEnd !== item.endDate && newEnd !== null){
                // controller expects GET query param `endDate`
                await updateTodoEndDate(item.id, newEnd);
            }
        } catch(err){ console.error(err); alert('Unable to save changes'); }

        closeCreateModal();
        await loadList();
    };

    modalCancel.onclick = () => closeCreateModal();
}

async function createTodo(title, context, endDate, status){
    try {
        const qs = [];
        if(title) qs.push('name=' + encodeURIComponent(title));
        if(context) qs.push('context=' + encodeURIComponent(context));
        if(typeof status !== 'undefined' && status !== null) qs.push('status=' + encodeURIComponent(status));
        if(endDate) qs.push('endDate=' + encodeURIComponent(endDate));
        const query = qs.length ? ('?' + qs.join('&')) : '';

        // POST /api/TodoNote/{listId}?name=...&context=...&status=...&endDate=...
        const res = await fetch(`${todoApi}/${encodeURIComponent(listId)}${query}`, { method: 'POST' });
        if(!res.ok){
            const t = await res.text().catch(()=>null);
            console.error('createTodo failed', res.status, t);
            throw new Error('create todo failed');
        }

        // the controller returns created Id as plain text
        const createdId = (await res.text()).trim();

        // add noteId to list: PATCH /api/TodoList/note/{listId} with body = noteId (JSON string)
        const addRes = await fetch(`${listsEndpoint}/note/${encodeURIComponent(listId)}`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(createdId)
        });
        if(!addRes.ok){
            const t = await addRes.text().catch(()=>null);
            console.error('Failed to add note to list', addRes.status, t);
            throw new Error('Failed to attach note to list');
        }

        await loadList();
    } catch(e){
        alert('Unable to add todo');
        console.error(e);
    }
}

async function updateTodoStatus(id, status){
    // new API: PATCH /api/TodoNote/{id}/status?status=...
    const url = `${todoApi}/${encodeURIComponent(id)}/status?status=${encodeURIComponent(status)}`;
    const res = await fetch(url, { method: 'PATCH' });
    if(!res.ok){
        const t = await res.text().catch(()=>null);
        console.error('updateTodoStatus failed', res.status, t);
        throw new Error('status update failed');
    }
}

async function updateTodoEndDate(id, endDateIso){
    // new API: PATCH /api/TodoNote/{id}/enddate?endDate=...
    const url = `${todoApi}/${encodeURIComponent(id)}/enddate?endDate=${encodeURIComponent(endDateIso)}`;
    const res = await fetch(url, { method: 'PATCH' });
    if(!res.ok){
        const t = await res.text().catch(()=>null);
        console.error('updateTodoEndDate failed', res.status, t);
        throw new Error('endDate update failed');
    }
}

async function updateTodoContent(id, content){
    // content patch route: PATCH /api/TodoNote/{id}/{content}  (content must be URL-safe)
    const encoded = encodeURIComponent(content);
    const url = `${todoApi}/${encodeURIComponent(id)}/${encoded}`;
    const res = await fetch(url, { method: 'PATCH' });
    if(!res.ok){
        const t = await res.text().catch(()=>null);
        console.error('updateTodoContent failed', res.status, t);
        throw new Error('content update failed');
    }
}

async function deleteTodo(id){
    // DELETE /api/TodoNote/{id}
    const res = await fetch(`${todoApi}/${encodeURIComponent(id)}`, { method:'DELETE' });
    if(!res.ok){ const t = await res.text().catch(()=>null); console.error('deleteTodo failed', res.status, t); throw new Error('delete failed'); }

    // remove id from list: DELETE /api/TodoList/note/{listId} with body = noteId
    const rem = await fetch(`${listsEndpoint}/note/${encodeURIComponent(listId)}`, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(id)
    });
    if(!rem.ok){
        const t = await rem.text().catch(()=>null);
        console.error('Failed to remove note from list', rem.status, t);
        throw new Error('failed to remove note from list');
    }
}

function openCreateModal(status){
    modalTitleInput.value = '';
    modalContextInput.value = '';
    modalEndInput.value = '';
    modalStatusSelect.value = String(status);
    modalBackdrop.style.display = 'flex';
    modalBackdrop.setAttribute('aria-hidden','false');
    modalTitleInput.focus();
}

function closeCreateModal(){
    modalBackdrop.style.display = 'none';
    modalBackdrop.setAttribute('aria-hidden','true');
}

modalCancel.addEventListener('click', ()=> closeCreateModal());
modalBackdrop.addEventListener('click', (e)=>{ if(e.target === modalBackdrop) closeCreateModal(); });

modalCreate.addEventListener('click', async ()=>{
    const title = modalTitleInput.value.trim();
    const context = modalContextInput.value.trim();
    const end = modalEndInput.value ? new Date(modalEndInput.value).toISOString() : null;
    const status = Number(modalStatusSelect.value) || VisibleStatuses[0];
    await createTodo(title, context, end, status);
    closeCreateModal();
});

function escapeHtml(str){
    if(!str) return '';
    return String(str).replace(/[&"'<>]/g, s => ({'&':'&amp;','"':'&quot;',"'":'&#39;','<':'&lt;','>':'&gt;'}[s]));
}

backBtn.addEventListener('click', ()=> window.location.href = '/MainPage');

// If the page sets listId via script tag, call loadList automatically.
// Otherwise caller should call setListId(...) then loadList().
if(listId) loadList();
