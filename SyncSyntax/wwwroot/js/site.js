// toggle show password
function togglePassword(inputId, iconElement) {
    const passwordInput = document.getElementById(inputId);
    const icon = iconElement.querySelector("i");

    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        icon.classList.remove("bi-eye");
        icon.classList.add("bi-eye-slash");
    } else {
        passwordInput.type = "password";
        icon.classList.remove("bi-eye-slash");
        icon.classList.add("bi-eye");
    }
}

//Sweet alert for delete category
document.addEventListener("DOMContentLoaded", function () {
    document.body.addEventListener('click', function (e) {
        const deleteBtn = e.target.closest('.btn-delete');
        if (!deleteBtn) return;

        const categoryId = deleteBtn.getAttribute('data-id');
        const row = deleteBtn.closest('tr');

        Swal.fire({
            title: 'Are you sure?',
            text: "This action cannot be undone.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                fetch('/Category/DeleteConfirm', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: new URLSearchParams({ id: categoryId })
                })
                    .then(response => response.json())
                    .then(data => {
                        if (data.success) {
                            Swal.fire('Deleted!', 'Category has been deleted.', 'success');
                            if (row) row.remove(); // Delete row from table
                        } else {
                            Swal.fire('Error!', data.message || 'Deletion failed.', 'error');
                        }
                    })
                    .catch(() => {
                        Swal.fire('Error!', 'Server error occurred.', 'error');
                    });
            }
        });
    });
});


//sweet alert for add category
document.addEventListener("DOMContentLoaded", function () {
    const addBtn = document.getElementById('btnAddCategory');
    if (!addBtn) return;

    addBtn.addEventListener('click', function () {
        Swal.fire({
            title: 'Add New Category',
            html:
                '<input id="swal-input-name" class="swal2-input" placeholder="category name">' +
                '<input id="swal-input-desc" class="swal2-input" placeholder="category description">',
            focusConfirm: false,
            showCancelButton: true,
            confirmButtonText: 'Save',
            cancelButtonText: 'Cancel',
            preConfirm: () => {
                const name = document.getElementById('swal-input-name').value.trim();
                const desc = document.getElementById('swal-input-desc').value.trim();

                if (!name) {
                    Swal.showValidationMessage('Category name is required..');
                    return false;
                }

                return { name: name, description: desc };
            }
        }).then((result) => {
            if (result.isConfirmed) {
                const data = result.value;

                fetch('/Category/Create', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(data)
                })
                    .then(res => {
                        if (!res.ok) throw new Error("HTTP error " + res.status);
                        return res.json();
                    })
                    .then(resData => {
                        if (resData.success) {
                            Swal.fire('Success', 'Category added successfully.', 'success')
                                .then(() => location.reload());
                        } else {
                            Swal.fire('Error', resData.message || 'Failed to add category.', 'error');
                        }
                    })
                    .catch(err => {
                        Swal.fire('Error', 'Server error occurred.', 'error');
                    });
            }
        });
    });
});


// sweet alert for edit category
document.addEventListener("DOMContentLoaded", function () {
    const editModal = new bootstrap.Modal(document.getElementById('editModal'));

    document.body.addEventListener('click', function (e) {
        const editBtn = e.target.closest('.btn-edit');
        if (!editBtn) return;

        const id = editBtn.getAttribute('data-id');

        fetch('/Category/Edit/' + id)
            .then(res => res.text())
            .then(html => {
                document.getElementById('editModalContent').innerHTML = html;
                editModal.show();
                bindEditSave();
            });
    });

    function bindEditSave() {
        const saveBtn = document.getElementById('btnSaveEdit');
        if (!saveBtn) return;

        saveBtn.addEventListener('click', function () {
            const form = document.getElementById('editCategoryForm');
            const formData = new FormData(form);

            fetch('/Category/Edit', {
                method: 'POST',
                body: new URLSearchParams(formData)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        editModal.hide();
                        Swal.fire('Success', 'Category updated successfully.', 'success')
                            .then(() => location.reload());
                    } else {
                        Swal.fire('Error', data.message || 'Failed to update category.', 'error');
                    }
                })
                .catch(() => {
                    Swal.fire('Error', 'Server error occurred.', 'error');
                });
        });
    }
});
