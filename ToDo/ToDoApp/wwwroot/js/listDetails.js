const VisibleStatuses = [1,2,3,4];
const statusNames = { 1:'Waiting',2:'In Process',3:'Disabled',4:'Completed' };

const listsEndpoint = '/api/TodoList';
const todoApi = '/api/TodoNote';

const board = document.getElementById('board');
const backBtn = document.getElementById('backBtn');
const listTitle = document.getElementById('listTitle');
const listMeta = document.getElementById('listMeta');

const modalBackdrop = document.getElementById('modalBackdrop');
const modalTitleInput = document.getElementById('modalTitleInput');
const modalContextInput = document.getElementById('modalContextInput');
const modalEndInput = document.getElementById('modalEndInput');
const modalStatusSelect = document.getElementById('modalStatusSelect');
const modalCancel = document.getElementById('modalCancel');
const modalCreate = document.getElementById('modalCreate');

let listId = null;
let modalMode = 'create'; 
let editingItem = null;

function setListId(id) {
    listId = id;
}

function escapeHtml(str){
    if(!str) return '';
    return String(str).replace(/[&"'<>]/g, s => ({'&':'&amp;','"':'&quot;',"'":'&#39;','<':'&lt;','>':'&gt;'}[s]));
}

function extractNoteIds(list){
    const raw = list?.noteIds ?? list?.NoteIds ?? list?.NoteIDs ?? list?.noteIDs ?? [];
    if (Array.isArray(raw)) return raw;
    if (raw && Array.isArray(raw.$values)) return raw.$values;
    return [];
}

function safeJsonParse(text){
    try { return JSON.parse(text); } catch { return null; }
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
        const res = await fetch(`${listsEndpoint}/${encodeURIComponent(listId)}`, { cache: 'no-store' });
        if(!res.ok) {
            const t = await res.text().catch(()=>null);
            throw new Error('failed to load list: ' + (t ?? res.status));
        }
        const list = await res.json();

        const name = list.name ?? list.Name ?? 'Unnamed';
        const noteIds = extractNoteIds(list);
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
        const ids = Array.isArray(noteIds) ? noteIds : [];

        if(ids.length === 0){
            renderBoard([]);
            return;
        }

        const fetches = ids.map(id => fetch(`${todoApi}/${encodeURIComponent(id)}`, { cache: 'no-store' })
            .then(r => {
                if(!r.ok) return r.text().then(t => { throw new Error(`Failed to load note ${id}: ${r.status} ${t}`); });
                return r.json();
            })
        );

        const rawNotes = await Promise.all(fetches);

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
    card.querySelector('.left').addEventListener('click', () => openEditModal(item));
    card.querySelector('.status-display').addEventListener('click', () => openEditModal(item));

    const titleEl = card.querySelector('.title');
    titleEl.addEventListener('blur', async (e) => {
        const newTitle = e.target.textContent.trim();
        if(newTitle && newTitle !== item.title){
            try { await updateTodoContent(item.id, newTitle); item.title = newTitle; } 
            catch(err){ console.error(err); e.target.textContent = item.title; alert('Unable to save title'); }
        } else e.target.textContent = item.title;
    });

    const contentEl = card.querySelector('.content');
    contentEl.addEventListener('blur', async (e) => {
        const newContent = e.target.textContent.trim();
        if(newContent !== item.content){
            try { await updateTodoContent(item.id, newContent); item.content = newContent; } 
            catch(err){ 
                console.error(err); 
                e.target.textContent = item.content ? (item.content.length > maxLength ? item.content.slice(0, maxLength) + '...' : item.content) : ''; 
                alert('Unable to save content');
            }
        } else {
            e.target.textContent = item.content ? (item.content.length > maxLength ? item.content.slice(0, maxLength) + '...' : item.content) : '';
        }
    });

    card.querySelector('.btn-delete').addEventListener('click', async (e) => {
        e.stopPropagation();
        if(!confirm('Delete this task?')) return;
        try{
            await deleteTodo(item.id);
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
        else buckets[1].push(it);
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

function openCreateModal(status){
    modalMode = 'create';
    editingItem = null;

    modalTitleInput.value = '';
    modalContextInput.value = '';
    modalEndInput.value = '';
    modalStatusSelect.value = String(status);
    modalBackdrop.style.display = 'flex';
    modalBackdrop.setAttribute('aria-hidden','false');
    modalTitleInput.focus();
}

function openEditModal(item){
    modalMode = 'edit';
    editingItem = item;

    modalTitleInput.value = item.title || '';
    modalContextInput.value = item.content || '';
    modalEndInput.value = item.endDate ? new Date(item.endDate).toISOString().slice(0,16) : '';
    modalStatusSelect.value = String(item.status || VisibleStatuses[0]);
    modalBackdrop.style.display = 'flex';
    modalBackdrop.setAttribute('aria-hidden','false');
    modalTitleInput.focus();
}

function closeCreateModal(){
    modalBackdrop.style.display = 'none';
    modalBackdrop.setAttribute('aria-hidden','true');
    modalMode = 'create';
    editingItem = null;
}

async function onModalCreateClick(){
    if (modalCreate.disabled) return;
    modalCreate.disabled = true;

    try {
        const title = modalTitleInput.value.trim();
        const context = modalContextInput.value.trim();
        const end = modalEndInput.value ? new Date(modalEndInput.value).toISOString() : null;
        const status = Number(modalStatusSelect.value) || VisibleStatuses[0];

        if (modalMode === 'create') {
            await createTodo(title, context, end, status);
            closeCreateModal();
            await loadList();
        } else if (modalMode === 'edit' && editingItem) {
            try {
                if(title !== editingItem.title) await updateTodoTitle(editingItem.id, title);
                if(context !== editingItem.content) await updateTodoContent(editingItem.id, context);
                if(status !== editingItem.status) await updateTodoStatus(editingItem.id, status);
                if(end !== editingItem.endDate && end !== null) await updateTodoEndDate(editingItem.id, end);
            } catch(err){
                console.error(err);
                alert('Unable to save changes');
            }
            closeCreateModal();
            await loadList();
        }
    } finally {
        setTimeout(()=> modalCreate.disabled = false, 150);
    }
}

modalCreate.removeEventListener('click', onModalCreateClick);
modalCreate.addEventListener('click', onModalCreateClick);
modalCancel.addEventListener('click', ()=> closeCreateModal());
modalBackdrop.addEventListener('click', (e)=>{ if(e.target === modalBackdrop) closeCreateModal(); });


async function createTodo(title, context, endDate, status){
    if(!listId) { alert('List ID not set'); return; }
    modalCreate.disabled = true;
    try {
        const payload = { name: title || '', content: context || '', status: status ?? VisibleStatuses[0], endDate: endDate ?? null };
        let res;

        try {
            res = await fetch(`${listsEndpoint}/${encodeURIComponent(listId)}/notes`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
        } catch(e) {
            res = null;
        }

        let createdId = null;
        if (res && res.ok) {
            const text = await res.text();
            const maybeJson = safeJsonParse(text);
            if (maybeJson && (maybeJson.id || maybeJson.Id)) createdId = (maybeJson.id ?? maybeJson.Id);
            else if (text && text.trim()) createdId = text.trim();
        } else {
            const qs = [];
            if(title) qs.push('name=' + encodeURIComponent(title));
            if(context) qs.push('context=' + encodeURIComponent(context));
            if(typeof status !== 'undefined' && status !== null) qs.push('status=' + encodeURIComponent(status));
            if(endDate) qs.push('endDate=' + encodeURIComponent(endDate));
            const query = qs.length ? ('?' + qs.join('&')) : '';

            const res2 = await fetch(`${todoApi}/${encodeURIComponent(listId)}${query}`, { method: 'POST' });
            if(!res2.ok){
                const t = await res2.text().catch(()=>null);
                console.error('createTodo failed', res2.status, t);
                throw new Error('create todo failed');
            }
            createdId = (await res2.text()).trim();
        }

        if(!createdId) throw new Error('Failed to obtain created note id');

        const addRes = await fetch(`${listsEndpoint}/note/${encodeURIComponent(listId)}`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(createdId)
        });

        if(!addRes.ok){
            const t = await addRes.text().catch(()=>null);
            console.error('Failed to add note to list', addRes.status, t);
            alert('Note created but failed to attach to list. It may already be attached or try reloading.');
        }

        await loadList();
    } catch(e){
        alert('Unable to add todo');
        console.error(e);
    } finally {
        modalCreate.disabled = false;
    }
}


async function updateTodoTitle(id, title){
    if(!id) throw new Error('id required');
    try {
        const res = await fetch(`${todoApi}/${encodeURIComponent(id)}/title?title=${encodeURIComponent(title)}`, { method: 'PATCH' });
        if (res.ok) return;
    } catch(e) {
    }

    const encoded = encodeURIComponent(content);
    const url = `${todoApi}/${encodeURIComponent(id)}/${encoded}`;
    const res2 = await fetch(url, { method: 'PATCH' });
    if(!res2.ok){
        const t = await res2.text().catch(()=>null);
        console.error('updateTodoContent failed', res2.status, t);
        throw new Error('content update failed');
    }
}


async function updateTodoContent(id, content){
    if(!id) throw new Error('id required');
    try {
        const res = await fetch(`${todoApi}/${encodeURIComponent(id)}/content`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ content })
        });
        if (res.ok) return;
    } catch(e) {
    }

    const encoded = encodeURIComponent(content);
    const url = `${todoApi}/${encodeURIComponent(id)}/${encoded}`;
    const res2 = await fetch(url, { method: 'PATCH' });
    if(!res2.ok){
        const t = await res2.text().catch(()=>null);
        console.error('updateTodoContent failed', res2.status, t);
        throw new Error('content update failed');
    }
}

async function updateTodoStatus(id, status){
    const url = `${todoApi}/${encodeURIComponent(id)}/status?status=${encodeURIComponent(status)}`;
    const res = await fetch(url, { method: 'PATCH' });
    if(!res.ok){
        const t = await res.text().catch(()=>null);
        console.error('updateTodoStatus failed', res.status, t);
        throw new Error('status update failed');
    }
}

async function updateTodoEndDate(id, endDateIso){
    const url = `${todoApi}/${encodeURIComponent(id)}/enddate?endDate=${encodeURIComponent(endDateIso)}`;
    const res = await fetch(url, { method: 'PATCH' });
    if(!res.ok){
        const t = await res.text().catch(()=>null);
        console.error('updateTodoEndDate failed', res.status, t);
        throw new Error('endDate update failed');
    }
}

async function deleteTodo(id){
    const res = await fetch(`${todoApi}/${encodeURIComponent(id)}`, { method:'DELETE' });
    if(!res.ok){ const t = await res.text().catch(()=>null); console.error('deleteTodo failed', res.status, t); throw new Error('delete failed'); }

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

modalCancel.addE\ventListener('click', ()=> closeCreateModal());
backBtn.addEventListener('click', ()=> window.location.href = '/MainPage');

if(listId) loadList();
