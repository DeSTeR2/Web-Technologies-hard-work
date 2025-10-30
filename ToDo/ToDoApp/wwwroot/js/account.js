async function loadUserInfo() {
    try {
        const res = await fetch('/api/ApiUserInfo', {cache: 'no-store'});
        if (!res.ok) throw new Error('Could not get user info');
        const user = await res.json();
        document.getElementById('userName').textContent = user.name;
    } catch (e) {
        console.error(e);
    }
}

document.getElementById('logoutBtn').addEventListener('click', async () => {
    try {
        const res = await fetch('/Account/Logout', {
            method: 'POST',
            credentials: 'include'
        });

        if (!res.ok) throw new Error('Logout failed');

        window.location.href = '/';
    } catch (e) {
        console.error(e);
        alert('Unable to logout');
    }
});

loadUserInfo();