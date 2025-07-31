document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.save-toggle-btn').forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();

            const postId = this.getAttribute('data-post-id');
            const returnPage = this.getAttribute('data-return-page');
            const token = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]')?.value;

            try {
                const response = await fetch('/ContentCreator/Post/ToggleSave', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': token
                    },
                    body: `postId=${postId}&returnPage=${returnPage}`
                });

                if (response.ok) {
                    const icon = this.querySelector('i');

                    if (icon.classList.contains('bi-bookmark')) {
                        icon.classList.remove('bi-bookmark');
                        icon.classList.add('bi-bookmark-fill');
                        this.setAttribute('data-hover-text', 'Un Save');
                        toastr.success("Post saved to your list.");
                    } else {
                        icon.classList.remove('bi-bookmark-fill');
                        icon.classList.add('bi-bookmark');
                        this.setAttribute('data-hover-text', 'Save');
                        toastr.info("Post removed from your saved list.");
                    }
                } else {
                    toastr.error("Toggle failed. Try again.");
                }
            } catch (error) {
                console.error("Fetch error:", error);
                toastr.error("An error occurred. Please try again.");
            }
        });
    });
});
