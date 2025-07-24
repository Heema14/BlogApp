document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.save-toggle-btn').forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();

            const postId = this.getAttribute('data-post-id');
            const returnPage = this.getAttribute('data-return-page');

            const token = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch('/ContentCreator/Post/ToggleSave', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: `postId=${postId}&returnPage=${returnPage}`
            });

            if (response.ok) {
                // Toggle icon and text
                const icon = this.querySelector('i');
 
                if (icon.classList.contains('bi-bookmark')) {
                    icon.classList.remove('bi-bookmark');
                    icon.classList.add('bi-bookmark-fill');
                    
                } else {
                    icon.classList.remove('bi-bookmark-fill');
                    icon.classList.add('bi-bookmark');
                 }
            } else {
                console.error("Toggle failed");
            }
        });
    });
});
