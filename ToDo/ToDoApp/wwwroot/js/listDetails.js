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

const editModalBackdrop = document.getElementById('editModalBackdrop');
const editModalTitle = document.getElementById('editModalTitle');
const editModalContext = document.getElementById('editModalContext');
const editModalStatus = document.getElementById('editModalStatus');
const editModalStart = document.getElementById('editModalStart');
const editModalEnd = document.getElementById('editModalEnd');
const editModalUpdate = document.getElementById('editModalUpdate');
const editModalCancel = document.getElementById('editModalCancel');
const modalDeleteBtn = document.getElementById('modalDeleteBtn');

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
            startDate: n.startDate ?? n.StartDate ?? null,
            endDate: n.endDate ?? n.EndDate ?? n.EndedAt ?? null,
            status: n.status ?? n.Status ?? 0,
            imageUrls: n.imageUrls ?? n.ImageUrls ?? []
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

    const maxLength = 20;
    const previewContent = item.content
        ? (item.content.length > maxLength ? item.content.slice(0, maxLength) + '...' : item.content)
        : '';

    card.innerHTML = `
        <div class="left">
            <div class="title" contenteditable="true">${escapeHtml(item.title)}</div>
            <div class="content">${escapeHtml(previewContent)}</div>
        </div>
    `;

    // Open **big edit modal** on card click
    card.addEventListener('click', (e) => {
        openEditModalBig(item);
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
                if(end !== editingItem.endDate && end !== null) await updateTodoDate(editingItem.id, end, Date.now());
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

async function updateTodoDate(id, endDateIso, startDateIso){
    const url = `${todoApi}/${encodeURIComponent(id)}/date?endDate=${encodeURIComponent(endDateIso)}&startDate=${encodeURIComponent(startDateIso)}`;
    const res = await fetch(url, { method: 'PATCH' });
    if(!res.ok){
        const t = await res.text().catch(()=>null);
        console.error('updateTodoDate failed', res.status, t);
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


// Edit modal elements


// Populate status dropdown
VisibleStatuses.forEach(s => {
    const opt = document.createElement('option');
    opt.value = s;
    opt.textContent = statusNames[s];
    editModalStatus.appendChild(opt);
});

let editingNote = null;

const imageDropZone = document.getElementById('imageDropZone');
const imageFileInput = document.getElementById('imageFileInput');
const imagePreview = document.getElementById('imagePreview');

// highlight on drag over
imageDropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    imageDropZone.classList.add('dragover');
});

imageDropZone.addEventListener('dragleave', () => {
    imageDropZone.classList.remove('dragover');
});

imageDropZone.addEventListener('drop', (e) => {
    e.preventDefault();
    imageDropZone.classList.remove('dragover');
    handleFiles(e.dataTransfer.files);
});

// click to open file dialog
imageDropZone.addEventListener('click', () => imageFileInput.click());

imageFileInput.addEventListener('change', (e) => handleFiles(e.target.files));

async function handleFiles(files) {
    if (!editingNote) return;

    // Clear existing previews to avoid duplicates
    imagePreview.innerHTML = '';

    for (const file of files) {
        const status = document.createElement('div');
        status.textContent = `Uploading ${file.name}...`;
        imagePreview.appendChild(status);

        const formData = new FormData();
        formData.append('file', file);

        try {
            const res = await fetch(`/api/AmazonS3/note/${encodeURIComponent(editingNote.id)}/upload`, {
                method: 'POST',
                body: formData
            });

            if (!res.ok) {
                status.textContent = `❌ Failed: ${file.name}`;
                continue;
            }

            const data = await res.json();
            const imageUrl = data.url ?? data.Url;
            uploadNoteImage(editingNote.id, imageUrl);

            // Replace the status element with the new image
            const img = document.createElement('img');
            img.src = imageUrl;
            img.alt = 'Uploaded image';
            img.style.width = '120px';
            img.style.height = '120px';
            img.style.objectFit = 'cover';
            img.style.borderRadius = '8px';
            img.style.cursor = 'pointer';
            img.title = 'Click to view full size';
            img.addEventListener('click', () => window.open(imageUrl, '_blank'));

            imagePreview.replaceChild(img, status);
        } catch (err) {
            console.error(err);
            status.textContent = `❌ Upload error: ${file.name}`;
        }
    }

    // Re-fetch images from S3 to refresh the list
    await fetchAndDisplayNoteImages(editingNote.id);
}



async function openEditModalBig(note) {
    editingNote = note;

    editModalTitle.textContent = note.title ?? '';
    editModalContext.value = note.content ?? '';
    editModalStatus.value = String(note.status ?? VisibleStatuses[0]);
    editModalStart.value = note.startDate ? new Date(note.startDate).toISOString().slice(0,16) : '';
    editModalEnd.value = note.endDate ? new Date(note.endDate).toISOString().slice(0,16) : '';

    imageFileInput.innerHTML = '';
    imagePreview.innerHTML = '';
    editModalBackdrop.style.display = 'flex';
    // Prepare image container (below context textarea)
    const imageContainerId = 'editModalImageContainer';
    let imageContainer = document.getElementById(imageContainerId);
    if (!imageContainer) {
        imageContainer = document.createElement('div');
        imageContainer.id = imageContainerId;
        imageContainer.style.display = 'flex';
        imageContainer.style.flexWrap = 'wrap';
        imageContainer.style.gap = '8px';
        imageContainer.style.marginTop = '10px';
        editModalContext.insertAdjacentElement('afterend', imageContainer);
    }

    // Clear previous content (important)
    imageContainer.innerHTML = 'Loading images...';

    // Fetch and display all images for this note
    await fetchAndDisplayNoteImages(note.id);
}

async function fetchAndDisplayNoteImages(noteId) {
    const container = document.getElementById('editModalImageContainer');
    if (!container) return;

    container.innerHTML = 'Loading images...';

    try {
        // First: try getting URLs from the TodoNote API (MongoDB)
        const res = await fetch(`/api/TodoNote/${encodeURIComponent(noteId)}/images`, {
            cache: 'no-store'
        });

        if (!res.ok) {
            const text = await res.text().catch(() => null);
            throw new Error(`Failed to fetch note images: ${text ?? res.statusText}`);
        }

        let json = await res.json();
        const imageUrls = json.$values;

        container.innerHTML = '';

        if (!Array.isArray(imageUrls) || imageUrls.length === 0) {
            return;
        }

        for (const url of imageUrls) {
            const img = document.createElement('img');
            img.src = url;
            img.alt = 'Note image';
            img.style.width = '120px';
            img.style.height = '120px';
            img.style.objectFit = 'cover';
            img.style.borderRadius = '8px';
            img.style.cursor = 'pointer';
            img.title = 'Click to view full size';
            img.addEventListener('click', () => window.open(url, '_blank'));
            container.appendChild(img);
        }

    } catch (err) {
        console.error('Failed to fetch note images:', err);

        // Fallback: try to list files directly from S3 if MongoDB didn’t return
        try {
            const res2 = await fetch(`/api/AmazonS3/note/${encodeURIComponent(noteId)}/files`);

            if (res2.ok) {
                const files = await res2.json();
                const noteFiles = files.filter(f => f.Key.includes(`/todo-notes/${noteId}/`));
                container.innerHTML = '';

                if (noteFiles.length === 0) {
                    return;
                }

                for (const file of noteFiles) {
                    const imgUrl = `https://centalbucketnametest.s3.eu-north-1.amazonaws.com/${file.Key}`;
                    const img = document.createElement('img');
                    img.src = imgUrl;
                    img.alt = 'Note image';
                    img.style.width = '120px';
                    img.style.height = '120px';
                    img.style.objectFit = 'cover';
                    img.style.borderRadius = '8px';
                    img.style.cursor = 'pointer';
                    img.title = 'Click to view full size';
                    img.addEventListener('click', () => window.open(imgUrl, '_blank'));
                    container.appendChild(img);
                }
            } else {
                container.textContent = 'Failed to load images.';
            }
        } catch (fallbackErr) {
            console.error('S3 fallback also failed:', fallbackErr);
            container.textContent = 'Failed to load images.';
        }
    }
}



async function uploadNoteImage(noteId, path) {

    const res = await fetch(`${todoApi}/${encodeURIComponent(noteId)}/image?filePath=${path}`, {
        method: 'POST'});

    if (!res.ok) {
        const text = await res.text().catch(()=>null);
        throw new Error('Failed to upload images: ' + (text ?? res.status));
    }

    return await res.json();
}


function closeEditModal(){
    editModalBackdrop.style.display = 'none';
    editingNote = null;
}

editModalCancel.addEventListener('click', closeEditModal);
editModalBackdrop.addEventListener('click', e => { if(e.target === editModalBackdrop) closeEditModal(); });
modalDeleteBtn.addEventListener('click', async () => {
    if(!editingItem) return;
    if(!confirm('Are you sure you want to delete this note?')) return;

    try {
        await deleteTodo(editingItem.id);
        closeCreateModal();
        await loadList();
    } catch(err) {
        console.error(err);
        alert('Unable to delete note');
    }
});

editModalUpdate.addEventListener('click', async () => {
    if(!editingNote) return;
    const newTitle = editModalTitle.textContent.trim();
    const ModalContext = editModalContext.value.trim();
    const newStatus = Number(editModalStatus.value);
    const newStart = editModalStart.value ? new Date(editModalStart.value).toISOString() : null;
    const newEnd = editModalEnd.value ? new Date(editModalEnd.value).toISOString() : null;

    try {
        if(newTitle !== editingNote.title) await updateTodoTitle(editingNote.id, newTitle);
        if(ModalContext !== editingNote.context) await updateTodoContent(editingNote.id, ModalContext);
        if(newStatus !== editingNote.status) await updateTodoStatus(editingNote.id, newStatus);
        if(newEnd !== editingNote.endDate && newEnd !== null) await updateTodoDate(editingNote.id, newEnd, newStart);
        // content is kept only in frontend, no need to save to DB
        editingNote.title = newTitle;
        editingNote.status = newStatus;
        editingNote.endDate = newEnd;
        editingNote.context = ModalContext;
    } catch(e){
        console.error(e);
        alert('Failed to update note.');
    } finally {
        closeEditModal();
        await loadList();
    }
});


imageDropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    e.stopPropagation();
    imageDropZone.classList.add('dragover');
});

imageDropZone.addEventListener('dragleave', (e) => {
    e.preventDefault();
    e.stopPropagation();
    imageDropZone.classList.remove('dragover');
});

imageDropZone.addEventListener('drop', (e) => {
    e.preventDefault();
    e.stopPropagation();
    imageDropZone.classList.remove('dragover');
    handleFiles(e.dataTransfer.files);
});


modalCancel.addEventListener('click', ()=> closeCreateModal());
backBtn.addEventListener('click', ()=> window.location.href = '/MainPage');

if(listId)
    loadList();
